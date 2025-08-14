using DG.Tweening;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using uMicrophoneWebGL;
using UnityEngine;
using UnityEngine.Networking;

public class STT_TTS_Manager : MonoBehaviour
{
    public static STT_TTS_Manager Instance { get; private set; }

    [Header("TTS & STT"), Space(10)]
    public MicrophoneWebGL microphoneWebGL;
    public AudioSource audioSource;
    public float maxDuration = 10f;
    private float[] _buffer = null;
    private int _bufferSize = 0;
    private AudioClip _clip;
    private bool _isPlaying = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //---------- Text to Speech (TTS) Functionality ----------//

    public IEnumerator TTS(string sentence)
    {
        WWWForm form = new WWWForm();
        form.AddField("text", sentence);

        string url = APIConfig.Instance.TtsUrl;
        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JSON.Parse(request.downloadHandler.text);
            string base64Audio = result["audio_base64"];
            byte[] audioBytes = Convert.FromBase64String(base64Audio);

#if UNITY_WEBGL && !UNITY_EDITOR
        AudioClip clip = WavToAudioClip(audioBytes, "TTS_AudioClip");
        audioSource.clip = clip;
        audioSource.Play();
#else
            //yield return StartCoroutine(PlayAudioFromBytes(audioBytes));
            //AudioClip clip = WavToAudioClip(audioBytes, "TTS_AudioClip");
            //audioSource.clip = clip;
            //audioSource.Play();

            yield return StartCoroutine(PlayAudioFromBytesWeb(base64Audio));
#endif

            // 재생이 끝날 때까지 대기
            yield return new WaitWhile(() => audioSource.isPlaying);
        }
        else
        {
            Debug.LogError("TTS 응답 오류: " + request.error);
        }
    }

    // editer conversion from byte[] to AudioClip
    IEnumerator PlayAudioFromBytes(byte[] data)
    {
        string path = Path.Combine(Application.persistentDataPath, "temp.mp3");
        File.WriteAllBytes(path, data);

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
                Debug.LogError("TTS 재생 오류: " + www.error);
            }
        }
    }

    // WebGl conversion from mp3 to AudioClip

    IEnumerator PlayAudioFromBytesWeb(string base64Audio)
    {
        // Base64 → data URI 형식으로 변환
        string dataUri = "data:audio/mp3;base64," + base64Audio;

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(dataUri, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
                audioSource.Play();
            }
            else
            {
                Debug.LogError("WebGL TTS 재생 오류: " + www.error);
            }
        }
    }


    [ContextMenu("Test TTS")]
    public void TestTTS()
    {
        string testSentence = "안녕하세요, 이것은 테스트 음성입니다.";
        StartCoroutine(TTS(testSentence));
    }



    //---------- Speech to Text (STT) Functionality ----------//

    public IEnumerator STT(string sentens)
    {

        yield return null;
    }


}
