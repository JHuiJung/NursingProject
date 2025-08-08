using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.IO;

public class STTChat : MonoBehaviour
{
    [Header("🎙️ UI 연결")]
    public TMP_Text statusText;
    public TMP_Text feedbackText;
    public TMP_Text questionText;
    public AudioSource audioSource;

    // API URL은 APIConfig에서 관리
    private AudioClip recordedClip;
    private bool isRecording = false;
    private const int sampleRate = 16000;
    private const int maxRecordingTime = 30;

    private int currentQuestionIndex = 0;
    private string[] questions = new string[]
    {
        "Q17. 환아의 이름과 등록번호를 확인하세요.",
        "Q18. 환아에게 이번 주사의 목적과 과정을 설명해주세요."
    };

    public void Start()
    {
        ShowCurrentQuestion();
    }

    public void StartRecording()
    {
        if (isRecording) return;

        statusText.text = "🎙️ 녹음 시작...";
        recordedClip = Microphone.Start(null, false, maxRecordingTime, sampleRate);
        isRecording = true;
        StartCoroutine(AutoStopRecordingAfterDelay(maxRecordingTime));
    }

    private IEnumerator AutoStopRecordingAfterDelay(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (isRecording) StopRecordingAndSend();
    }

    public void StopRecordingAndSend()
    {
        if (!isRecording) return;

        Microphone.End(null);
        isRecording = false;
        statusText.text = "⏱️ 녹음 완료, 서버로 전송 중...";

        StartCoroutine(SendWavToServer(recordedClip, questions[currentQuestionIndex]));
    }

    IEnumerator SendWavToServer(AudioClip clip, string question)
    {
        string filePath = Path.Combine(Application.persistentDataPath, "recorded.wav");
        SaveWav(filePath, clip);
        byte[] audioData = File.ReadAllBytes(filePath);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("question", question),
            new MultipartFormFileSection("audio", audioData, "recorded.wav", "audio/wav")
        };

        UnityWebRequest request = UnityWebRequest.Post(APIConfig.Instance.ClovaSttUrl, formData);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log("✅ 응답 수신: " + json);

            STTResponse response = JsonUtility.FromJson<STTResponse>(json);
            statusText.text = "✅ 피드백 수신 완료";
            feedbackText.text = $"[{questions[currentQuestionIndex]}]\n📤 {response.transcript}\n🧠 {response.feedback}";

            NextQuestion();
        }
        else
        {
            statusText.text = "❌ 전송 실패: " + request.error;
            Debug.LogError("전송 실패: " + request.error);
        }
    }

    void NextQuestion()
    {
        currentQuestionIndex++;
        if (currentQuestionIndex < questions.Length)
        {
            ShowCurrentQuestion();
        }
        else
        {
            statusText.text = "✅ 모든 질문 완료";
            questionText.text = "";
        }
    }

    void ShowCurrentQuestion()
    {
        questionText.text = questions[currentQuestionIndex];
        feedbackText.text = "";
        statusText.text = "📋 질문 확인 후 녹음을 시작하세요.";
    }

    void SaveWav(string path, AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);
        byte[] wavData = WavUtility.FromAudioClip(clip, out _, true);
        File.WriteAllBytes(path, wavData);
    }

    [System.Serializable]
    public class STTResponse
    {
        public string transcript;
        public string feedback;
        public string question; 
    }
}