using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class NursingChatClient : MonoBehaviour
{

    [Header("🩺 UI 연결")]
    public TMP_InputField questionInput;   // Unity Inspector에 Drag & Drop
    public TMP_Text answerOutput;          // Unity Inspector에 Drag & Drop

    private const string apiUrl = "http://127.0.0.1:8000/chat"; // FastAPI 서버 주소

    [System.Serializable]
    public class ChatRequest
    {
        public string session_id;
        public string question;
    }

    [System.Serializable]
    public class ChatResponse
    {
        public string answer;
        public int correct_count;
        public int incorrect_count;
        public int total_questions;
        public float score_percentage;
    }

    // 버튼에서 호출될 함수
    public void OnSendButtonClick()
    {
        if (string.IsNullOrEmpty(questionInput.text))
        {
            answerOutput.text = "질문을 입력해 주세요.";
            return;
        }

        StartCoroutine(SendQuestionToAPI(questionInput.text));
    }

    public IEnumerator SendQuestionToAPI(string question)
    {
        ChatRequest requestData = new ChatRequest
        {
            session_id = "unity-session-001", // 나중에 사용자 고유 ID로 대체 가능
            question = question
        };

        string jsonData = JsonUtility.ToJson(requestData);
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(postData);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(www.downloadHandler.text);
                
                // 답변과 점수 정보를 함께 표시
                string displayText = response.answer;
                if (response.total_questions > 0)
                {
                    displayText += $"\n\n📊 점수: {response.correct_count}개 정답, {response.incorrect_count}개 오답";
                    displayText += $"\n🎯 정답률: {response.score_percentage}%";
                }
                
                answerOutput.text = displayText;
                Debug.Log($"✅ 응답 수신:\n{response.answer}\n📊 정답: {response.correct_count}개, 오답: {response.incorrect_count}개, 총: {response.total_questions}%");
            }
            else
            {
                Debug.LogError("❌ 요청 실패: " + www.error);
                answerOutput.text = "서버 오류: " + www.error;
            }
        }
    }

    
    public IEnumerator SendQuestionToAPIUsing(string question)
    {
        ChatRequest requestData = new ChatRequest
        {
            session_id = "unity-session-001", // 나중에 사용자 고유 ID로 대체 가능
            question = question
        };

        string jsonData = JsonUtility.ToJson(requestData);
        byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(postData);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                ChatResponse response = JsonUtility.FromJson<ChatResponse>(www.downloadHandler.text);
                
                // 답변과 점수 정보를 함께 표시
                string displayText = response.answer;
                if (response.total_questions > 0)
                {
                    displayText += $"\n\n📊 점수: {response.correct_count}개 정답, {response.incorrect_count}개 오답";
                    displayText += $"\n🎯 정답률: {response.score_percentage}%";
                }
                
                answerOutput.text = displayText;
                Debug.Log($"✅ 응답 수신:\n{response.answer}\n📊 정답: {response.correct_count}개, 오답: {response.incorrect_count}개, 점수: {response.score_percentage}%");
            }
            else
            {
                Debug.LogError("❌ 요청 실패: " + www.error);
                answerOutput.text = "서버 오류: " + www.error;
            }
        }
    }


    // 선택적으로 Start에 초기화 메시지 넣을 수 있음
    void Start()
    {
        if (answerOutput != null)
        {
            answerOutput.text = "💬 질문을 입력하고 버튼을 눌러주세요.";
        }
    }
}