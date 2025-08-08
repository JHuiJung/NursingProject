// ✅ 수정 요약:
// 1. URL 부분 실제 서버 주소로 교체
// 2. Send/Request 오류 시 로그 추가
// 3. AudioClip null 검사 추가
// 4. SimpleJSON 없을 경우 대비
// 5. 질문 시작 시 TTS로 읽어주는 기능 추가
// 6. followup 질문 후 다시 답변할 수 있도록 버튼 상태 제어 추가

using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System;
using System.Collections;
using System.IO;
using TMPro;
using SimpleJSON;

public class ParentChatManager : MonoBehaviour
{
    public AudioSource audioSource;
    public TextMeshProUGUI questionText, transcriptText, feedbackText, keywordText;
    public Button recordBtn, stopBtn, sendBtn, nextBtn, summaryBtn;
    private bool isFollowupMode = false;
    public TextMeshProUGUI followupGuideText; // 새로 추가

    private AudioClip recordedClip;
    private string sessionId;
    private int questionIndex = 0;

    private readonly string[] questionIds = { "Q1", "Q2", "Q3", "Q4", "Q5" };
    private readonly string[] questions = {
        "이 약은 무슨 약인가요?",
        "약으로 먹진 않고 주사로만 투여되나요? 주사바늘을 또 맞는 건가요?",
        "항생제를 맞는다면 언제부터 효과가 나타날까요. 바로 감염수치가 낮아지나요?",
        "저에겐 어렵게 얻은 아이라 너무 걱정이 되는데요 약물 투여시 부작용은 없는거죠?",
        "혹시라도 방금 이야기해준 가벼운 부작용이나 아니면 심각한 알레르기 증상이 나타나면 어떻 처치를 해주나요?"
    };

    void Start()
    {
        sessionId = Guid.NewGuid().ToString();
        questionText.text = questions[questionIndex];
        stopBtn.interactable = false;
        sendBtn.interactable = false;
        nextBtn.interactable = false;
        StartCoroutine(PlayTTSQuestion(questions[questionIndex]));
    }

    public void OnRecordButton()
    {
        Debug.Log("▶ Record button pressed");
        recordedClip = Microphone.Start(null, false, 60, 44100);

        if (recordedClip == null)
        {
            Debug.LogWarning("⚠️ 녹음 장치가 없습니다.");
            return;
        }

        recordBtn.interactable = false;
        stopBtn.interactable = true;
    }

    public void OnStopButton()
    {
        Microphone.End(null);
        stopBtn.interactable = false;
        sendBtn.interactable = true;
    }

    public void OnSendButton()
    {
        if (isFollowupMode)
        {
            Debug.Log("📤 후속 답변 서버 전송");
            StartCoroutine(SendFollowupToServer());
            isFollowupMode = false; // ✅ 후속 전송 완료 후 기본 모드로
        }
        else
        {
            Debug.Log("📤 일반 질문 서버 전송");
            StartCoroutine(SendToServer());
        }
    }    

    public void OnNextButton()
    {
        questionIndex++;
        if (questionIndex < questions.Length)
        {
            questionText.text = questions[questionIndex];
            transcriptText.text = "";
            keywordText.text = "";
            followupGuideText.text = "";
            followupGuideText.gameObject.SetActive(false);  // 🔸 초기화
            sendBtn.interactable = false;
            recordBtn.interactable = true;
            nextBtn.interactable = false;

            Debug.Log("➡️ 다음 질문으로 이동: " + questions[questionIndex]);

            StartCoroutine(PlayTTSQuestion(questions[questionIndex]));
        }
        else
        {
            summaryBtn.interactable = true;
            questionText.text = "모든 질문이 완료되었습니다.";
        }
    }
    public void OnSummaryButton()
    {
        StartCoroutine(RequestSummary());
    }

    IEnumerator SendToServer()
    {
        if (recordedClip == null)
        {
            Debug.LogError("❌ AudioClip이 비어 있습니다.");
            yield break;
        }

        int length;
        byte[] wavData = WavUtility.FromAudioClip(recordedClip, out length);

        WWWForm form = new WWWForm();
        form.AddField("session_id", sessionId);
        form.AddField("question_id", questionIds[questionIndex]);
        form.AddBinaryData("audio", wavData, "audio.wav", "audio/wav");

        string url = APIConfig.Instance.ParentChatUrl;

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JSON.Parse(request.downloadHandler.text);
            string transcript = result["transcript"];
            transcriptText.text = transcript;

            Debug.Log("📝 사용자 질문 STT 결과: " + transcript);

            // ✅ [이 아래에 넣으세요]
            if (result["followup_needed"].AsBool)
            {
                string base64Audio = result["followup_audio_base64"];
                byte[] audioBytes = Convert.FromBase64String(base64Audio);
                PlayAudioFromBytes(audioBytes);

                // 텍스트 가이드가 함께 오면 표시
                string followupGuide = result.HasKey("followup_text") ? result["followup_text"] : "";
                keywordText.text = $"누락 키워드: {result["missing_keywords"]}";

                if (!string.IsNullOrEmpty(followupGuide))
                {
                    Debug.Log("✅ followup_text 수신됨: " + followupGuide);
                    followupGuideText.text = "💬 가이드: " + followupGuide;
                    followupGuideText.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning("⚠️ followup_text가 비어 있음");
                    followupGuideText.text = "";
                    followupGuideText.gameObject.SetActive(false);
                }

                isFollowupMode = true;

                recordBtn.interactable = true;
                stopBtn.interactable = true;
                sendBtn.interactable = true;
            }           
             else
            {
                // 키워드 모두 포함됨: 서버가 이해 확인용 TTS/텍스트를 내려줄 수 있음
                if (result.HasKey("ack_audio_base64"))
                {
                    byte[] ackBytes = Convert.FromBase64String(result["ack_audio_base64"]);
                    PlayAudioFromBytes(ackBytes);
                }
                if (result.HasKey("ack_text"))
                {
                    keywordText.text = result["ack_text"];
                }
                else
                {
                    keywordText.text = "키워드 모두 포함됨!";
                }

                isFollowupMode = false; // ✅ 기본 질문 모드
                nextBtn.interactable = true;

                recordBtn.interactable = true;
                stopBtn.interactable = true;
                sendBtn.interactable = true;
            }
        }
        else
        {
            Debug.LogError("❌ 서버 전송 실패: " + request.error);
        }
    }

    IEnumerator RequestSummary()
    {
        WWWForm form = new WWWForm();
        form.AddField("session_id", sessionId);

        string url = APIConfig.Instance.ParentChatSummaryUrl;
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JSON.Parse(request.downloadHandler.text);

            if (result.HasKey("feedback"))
            {
                string feedback = result["feedback"]["answer"];
                Debug.Log("📣 요약 피드백: " + feedback);
                feedbackText.text = string.IsNullOrEmpty(feedback) ? "⚠️ 피드백이 비어 있습니다." : feedback;
            }
            else
            {
                Debug.LogWarning("⚠️ 'feedback' 키 없음. 응답 내용: " + result.ToString());
                feedbackText.text = "⚠️ 서버에서 피드백을 받지 못했습니다.";
            }
        }
        else
        {
            feedbackText.text = "요약 실패: " + request.error;
            Debug.LogError("❌ 요약 요청 실패: " + request.error);
        }
    }

    void PlayAudioFromBytes(byte[] data)
    {
        string path = Path.Combine(Application.persistentDataPath, "temp.mp3");
        File.WriteAllBytes(path, data);
        StartCoroutine(PlayAudioFromFile(path));
    }

    IEnumerator PlayAudioFromFile(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.Play();
            }
            else
            {
                Debug.LogError("❌ TTS 오디오 로드 실패: " + www.error);
            }
        }
    }

    IEnumerator PlayTTSQuestion(string questionText)
    {
        WWWForm form = new WWWForm();
        form.AddField("text", questionText);

        string url = APIConfig.Instance.TtsUrl;
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JSON.Parse(request.downloadHandler.text);
            string base64Audio = result["audio_base64"];
            byte[] audioBytes = Convert.FromBase64String(base64Audio);
            PlayAudioFromBytes(audioBytes);
        }
        else
        {
            Debug.LogError("❌ 질문 TTS 요청 실패: " + request.error);
        }
    }

    IEnumerator SendFollowupToServer()
    {
        if (recordedClip == null)
        {
            Debug.LogError("❌ 후속질문 AudioClip이 비어 있음");
            yield break;
        }

        int length;
        byte[] wavData = WavUtility.FromAudioClip(recordedClip, out length);

        WWWForm form = new WWWForm();
        form.AddField("session_id", sessionId);
        form.AddField("question_id", questionIds[questionIndex]);
        form.AddBinaryData("audio", wavData, "followup.wav", "audio/wav");
        
        string url = APIConfig.Instance.ParentChatFollowupUrl;
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JSON.Parse(request.downloadHandler.text);
            string followupText = result["followup_text"];
            transcriptText.text += "\n(추가 답변) " + followupText;
            Debug.Log("✅ 후속 답변 저장 완료: " + followupText);
            nextBtn.interactable = true;
        }
        else
        {
            Debug.LogError("❌ 후속 답변 서버 전송 실패: " + request.error);
        }
    }
}