using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class STTChat : MonoBehaviour
{
    [Header("🖥 UI")]
    public TMP_Text statusText;
    public TMP_Text feedbackText;
    public TMP_Text questionText;

    [Header("Q17 입력 필드")]
    public TMP_InputField patientNameInput;
    public TMP_InputField patientRegNoInput;

    [Header("오디오")]
    public AudioSource audioSource;

    // ---- 내부 상태 ----
    private AudioClip recordedClip;
    private bool isRecording = false;
    private const int sampleRate = 16000;
    private const int maxRecordingTime = 30;

    private int currentQuestionIndex = 0; // 0: Q17, 1: Q18
    private readonly string[] questions = new string[]
    {
        "Q17. 환아의 이름과 등록번호를 확인하세요.",
        "Q18. 환아에게 이번 주사의 목적과 과정을 설명해주세요."
    };

    void Start()
    {
        ShowCurrentQuestion();
    }

    // =========================
    // 버튼 핸들러 (UI에서 연결)
    // =========================

    // Q17: “Feedback/확인” 버튼 → 텍스트 입력 전송
    public void OnClickFeedbackSend()
    {
        if (currentQuestionIndex != 0)
        {
            SetStatus("⚠️ 피드백 전송은 Q17에서만 가능합니다.");
            return;
        }
        StartCoroutine(SendTextInputsToServer());
    }

    // Q18: 녹음 시작
    public void OnClickRecord()
    {
        if (currentQuestionIndex != 1)
        {
            SetStatus("⚠️ 녹음은 Q18에서만 가능합니다.");
            return;
        }
        StartRecording();
    }

    // Q18: 녹음 정지
    public void OnClickStop()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        
#else
        if (!isRecording) return;
        Microphone.End(null);
        isRecording = false;
        SetStatus("⏹️ 녹음 종료");

#endif
    }

    // Q18: 오디오 전송
    public void OnClickSendAudio()
    {
        if (currentQuestionIndex != 1)
        {
            SetStatus("⚠️ 오디오 전송은 Q18에서만 가능합니다.");
            return;
        }
        if (recordedClip == null)
        {
            SetStatus("⚠️ 전송할 녹음이 없습니다.");
            return;
        }
        StartCoroutine(SendWavToServer(recordedClip, questions[currentQuestionIndex]));
    }

    // =========================
    // 녹음 컨트롤
    // =========================
    private void StartRecording()
    {
        if (isRecording) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        
#else
        SetStatus("🎙️ 녹음 시작...");
        recordedClip = Microphone.Start(null, false, maxRecordingTime, sampleRate);
        isRecording = true;
        StartCoroutine(AutoStopRecordingAfterDelay(maxRecordingTime)); // 자동 종료만; 전송은 수동
#endif
    }

    private IEnumerator AutoStopRecordingAfterDelay(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (isRecording)
        {
            OnClickStop(); // 종료만 수행
        }
    }

    // =========================
    // 서버 전송: Q18 (STT)
    // =========================
    private IEnumerator SendWavToServer(AudioClip clip, string question)
    {
        SetStatus("⏱️ 오디오 인코딩 후 전송 중...");
        string filePath = Path.Combine(Application.persistentDataPath, "recorded.wav");
        SaveWav(filePath, clip);
        byte[] audioData = File.ReadAllBytes(filePath);

        var formData = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("question", question),
            new MultipartFormFileSection("audio", audioData, "recorded.wav", "audio/wav")
        };

        using (var request = UnityWebRequest.Post(APIConfig.Instance.ClovaSttUrl, formData))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("✅ 응답 수신: " + json);

                var response = JsonUtility.FromJson<STTResponse>(json);
                SetStatus("✅ 피드백 수신 완료");
                SetFeedback($"[{questions[currentQuestionIndex]}]\n📤 {response.transcript}\n🧠 {response.feedback}");

                NextQuestion();
            }
            else
            {
                SetStatus("❌ 전송 실패: " + request.error);
                Debug.LogError("전송 실패: " + request.error);
            }
        }
    }

    // =========================
    // 서버 전송: Q17 (텍스트 입력)
    // =========================
    private IEnumerator SendTextInputsToServer()
    {
        string nameVal = patientNameInput != null ? patientNameInput.text : string.Empty;
        string regVal = patientRegNoInput != null ? patientRegNoInput.text : string.Empty;

        SetStatus("⏱️ 입력 전송 중...");

        var formData = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("question", questions[0]),
            new MultipartFormDataSection("patient_name", nameVal ?? string.Empty),
            new MultipartFormDataSection("patient_regno", regVal ?? string.Empty)
        };

        using (var request = UnityWebRequest.Post(APIConfig.Instance.ClovaSttUrl, formData))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                Debug.Log("✅ 텍스트 기반 응답 수신: " + json);

                var response = JsonUtility.FromJson<STTResponse>(json);
                SetStatus("✅ 입력 확인 완료");
                SetFeedback($"[{questions[0]}]\n👤 {response.transcript}\n🧠 {response.feedback}");

                NextQuestion();
            }
            else
            {
                SetStatus("❌ 전송 실패: " + request.error);
                Debug.LogError("전송 실패: " + request.error);
            }
        }
    }

    // =========================
    // 화면/유틸
    // =========================
    private void NextQuestion()
    {
        currentQuestionIndex++;
        if (currentQuestionIndex < questions.Length)
        {
            ShowCurrentQuestion();
        }
        else
        {
            SetStatus("✅ 모든 질문 완료");
            if (questionText) questionText.text = "";
        }
    }

    private void ShowCurrentQuestion()
    {
        if (questionText) questionText.text = questions[currentQuestionIndex];
        if (feedbackText) feedbackText.text = "";
        SetStatus("📋 질문 확인 후 진행하세요.");

        // Q17이면 이름/등록번호 필드 초기화
        if (currentQuestionIndex == 0)
        {
            if (patientNameInput) patientNameInput.text = "";
            if (patientRegNoInput) patientRegNoInput.text = "";
        }
    }

    private void SaveWav(string path, AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);
        byte[] wavData = WavUtility.FromAudioClip(clip, out _, true);
        File.WriteAllBytes(path, wavData);
    }

    private void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
    }

    private void SetFeedback(string msg)
    {
        if (feedbackText) feedbackText.text = msg;
    }

    // =========================
    // 서버 응답 DTO
    // =========================
    [System.Serializable]
    public class STTResponse
    {
        public string transcript;
        public string feedback;
        public string question;
    }
}