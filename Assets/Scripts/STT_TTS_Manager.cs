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
            yield return StartCoroutine(PlayAudioFromBytes(audioBytes));

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

    // WebGl conversion from wav to AudioClip
    public static AudioClip WavToAudioClip(byte[] wavFile, string clipName = "wavClip")
    {
        int channels = wavFile[22]; // 채널 수
        int sampleRate = BitConverter.ToInt32(wavFile, 24);
        int byteRate = BitConverter.ToInt32(wavFile, 28);
        int bitsPerSample = wavFile[34];

        Debug.Log($"WAV Info - channels: {channels}, sampleRate: {sampleRate}, bitsPerSample: {bitsPerSample}");

        int subchunk2 = BitConverter.ToInt32(wavFile, 40);
        int dataPos = 44;

        int bytesPerSample = bitsPerSample / 8;
        if (bytesPerSample == 0)
        {
            Debug.LogError("Invalid bitsPerSample in WAV data, cannot proceed.");
            return null;
        }

        int samples = subchunk2 / bytesPerSample;

        float[] data = new float[samples];
        int offset = dataPos;
        for (int i = 0; i < samples; i++)
        {
            if (offset + 1 >= wavFile.Length)
            {
                Debug.LogWarning("Unexpected end of WAV data.");
                break;
            }
            short sample = BitConverter.ToInt16(wavFile, offset);
            data[i] = sample / 32768.0f;
            offset += 2;
        }

        if (channels == 0 || sampleRate == 0)
        {
            Debug.LogError("Invalid WAV header values for channels or sampleRate.");
            return null;
        }

        AudioClip audioClip = AudioClip.Create(clipName, samples / channels, channels, sampleRate, false);
        audioClip.SetData(data, 0);

        return audioClip;
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
