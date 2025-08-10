using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.IO;

public class VoiceChatClient : MonoBehaviour
{
    [Header("🎙️ UI 연결")]
    public TMP_Text statusText;
    public AudioSource audioSource;

    // API URL은 APIConfig에서 관리
    private AudioClip recordedClip;
    private bool isRecording = false;
    private const int sampleRate = 16000;

    public void StartRecording()
    {
        if (isRecording) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        
#else
        statusText.text = "🎙️ 녹음 시작...";
        recordedClip = Microphone.Start(null, false, 5, sampleRate);
        isRecording = true;
#endif
    }

    public void StopRecordingAndSend()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        
#else
        if (!isRecording) return;

        Microphone.End(null);
        isRecording = false;
        statusText.text = "⏱️ 녹음 완료, 전송 중...";

        StartCoroutine(SendWavToServer(recordedClip));
#endif
    }

    IEnumerator SendWavToServer(AudioClip clip)
    {
        string filePath = Path.Combine(Application.persistentDataPath, "recorded.wav");
        SaveWav(filePath, clip);

        byte[] audioData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddField("session_id", "unity-session-001");
        form.AddBinaryData("audio", audioData, "voice.wav", "audio/wav");

        UnityWebRequest www = UnityWebRequest.Post(APIConfig.Instance.VoiceChatUrl, form);
        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            byte[] mp3Data = www.downloadHandler.data;
            string tempPath = Path.Combine(Application.persistentDataPath, "response.mp3");
            File.WriteAllBytes(tempPath, mp3Data);
            statusText.text = "✅ 음성 응답 수신됨";

            using (UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
            {
                yield return audioRequest.SendWebRequest();

                if (audioRequest.result == UnityWebRequest.Result.Success)
                {
                    AudioClip responseClip = DownloadHandlerAudioClip.GetContent(audioRequest);
                    audioSource.clip = responseClip;
                    audioSource.Play();
                    statusText.text = "▶ 음성 응답 재생 중...";
                }
                else
                {
                    statusText.text = "❌ mp3 재생 실패";
                    Debug.LogError("재생 실패: " + audioRequest.error);
                }
            }
        }
        else
        {
            statusText.text = "❌ 전송 실패: " + www.error;
            Debug.LogError("전송 오류: " + www.error);
        }
    }

    // ✅ 여기에 있어야 함 (클래스 바깥 아님!)
    void SaveWav(string path, AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);

        byte[] wavData = WavUtility.FromAudioClip(clip, out _, true);
        File.WriteAllBytes(path, wavData);
    }
}