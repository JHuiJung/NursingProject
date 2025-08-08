using UnityEngine;

[CreateAssetMenu(fileName = "APIConfig", menuName = "Nursing Project/API Config")]
public class APIConfig : ScriptableObject
{
    [Header("🌐 API 서버 설정")]
    [SerializeField] private string baseUrl = "https://c2a142ace12e.ngrok-free.app";
    
    [Header("📡 API 엔드포인트")]
    [SerializeField] private string chatEndpoint = "/chat";
    [SerializeField] private string parentChatEndpoint = "/parent_chat";
    [SerializeField] private string parentChatFollowupEndpoint = "/parent_chat/followup";
    [SerializeField] private string parentChatSummaryEndpoint = "/parent_chat/summary";
    [SerializeField] private string ttsEndpoint = "/tts";
    [SerializeField] private string clovaSttEndpoint = "/clova_stt";
    [SerializeField] private string voiceChatEndpoint = "/voice_chat";

    // 싱글톤 인스턴스
    private static APIConfig _instance;
    public static APIConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<APIConfig>("APIConfig");
                if (_instance == null)
                {
                    Debug.LogError("❌ APIConfig를 찾을 수 없습니다. Assets/Resources 폴더에 APIConfig.asset 파일이 있는지 확인해주세요.");
                }
            }
            return _instance;
        }
    }

    // URL 프로퍼티들
    public string ChatUrl => baseUrl + chatEndpoint;
    public string ParentChatUrl => baseUrl + parentChatEndpoint;
    public string ParentChatFollowupUrl => baseUrl + parentChatFollowupEndpoint;
    public string ParentChatSummaryUrl => baseUrl + parentChatSummaryEndpoint;
    public string TtsUrl => baseUrl + ttsEndpoint;
    public string ClovaSttUrl => baseUrl + clovaSttEndpoint;
    public string VoiceChatUrl => baseUrl + voiceChatEndpoint;

    // 개발/운영 환경 전환을 위한 메서드
    public void SetBaseUrl(string newBaseUrl)
    {
        baseUrl = newBaseUrl;
        Debug.Log($"🌐 API 서버 URL이 변경되었습니다: {baseUrl}");
    }

    // 현재 설정 정보 출력
    public void LogCurrentConfig()
    {
        Debug.Log($"🌐 현재 API 설정:\n" +
                  $"Base URL: {baseUrl}\n" +
                  $"Chat: {ChatUrl}\n" +
                  $"Parent Chat: {ParentChatUrl}\n" +
                  $"TTS: {TtsUrl}\n" +
                  $"STT: {ClovaSttUrl}\n" +
                  $"Voice Chat: {VoiceChatUrl}");
    }
}

