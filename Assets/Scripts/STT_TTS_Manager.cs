using DG.Tweening;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using uMicrophoneWebGL;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class STT_TTS_Manager : MonoBehaviour
{
    public static STT_TTS_Manager inst { get; private set; }

    [Header("Stuff"), Space(10)]
    public GameObject obj_Area_Wait;

    [Header("TTS & STT"), Space(10)]
    public MicrophoneWebGL microphoneWebGL;
    public AudioSource audioSource;
    public List<Device> devices = new List<Device>();
    [TextArea]
    public string stt_Text = "";
    public float maxDuration = 10f;
    public bool isMRecording = false;
    private float[] _buffer = null;
    private int _bufferSize = 0;
    private AudioClip _clip;

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
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

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var result = JSON.Parse(request.downloadHandler.text);
                string base64Audio = result["audio_base64"];
                byte[] audioBytes = Convert.FromBase64String(base64Audio);

                if (audioSource.clip != null)
                    Destroy(audioSource.clip); // 이전 클립 해제

                AudioClip clip = WavToAudioClip(audioBytes, "TTS_AudioClip");
                audioSource.clip = clip;
                audioSource.Play();

                yield return new WaitWhile(() => audioSource.isPlaying);

                // 재생이 끝난 후 클립 삭제
#if UNITY_WEBGL && !UNITY_EDITOR
            if (audioSource.clip != null)
            {
                audioSource.clip.UnloadAudioData();
                audioSource.clip = null;
            }
#else
                if (audioSource.clip != null)
                {
                    Destroy(audioSource.clip);
                    audioSource.clip = null;
                }
#endif
            }
            else
            {
                Debug.LogError("TTS 응답 오류: " + request.error);
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

        //Debug.Log($"WAV Info - channels: {channels}, sampleRate: {sampleRate}, bitsPerSample: {bitsPerSample}");

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

    public void ToggleRecord()
    {

        if (!microphoneWebGL || !microphoneWebGL.isValid) return;

        isMRecording = microphoneWebGL.isRecording;

        if (!isMRecording)
        {
            Begin();
        }
        else
        {
            End();
        }

        isMRecording = !isMRecording;
    }

    private void Begin()
    {
       // print($"{name} : 녹음 시작");
        microphoneWebGL.Begin();
    }

    private void End()
    {
        //print($"{name} : 녹음 끝");
        microphoneWebGL.End();
        StartCoroutine(STT(_clip));
    }

    public void OnBegin()
    {
        int freq = microphoneWebGL.selectedDevice.sampleRate;
        int n = (int)(freq * maxDuration);
        if (_buffer == null || _buffer.Length != n)
        {
            _buffer = new float[n];
        }
        _bufferSize = 0;
    }

    public void OnEnd()
    {
        if (!audioSource) return;

        var device = microphoneWebGL.selectedDevice;
        var freq = device.sampleRate;
        var ch = device.channelCount;
        _clip = AudioClip.Create("uMicrophoneWebGL-Recorded", _bufferSize + freq, ch, freq, false);
        var data = new float[_bufferSize];
        System.Array.Copy(_buffer, data, _bufferSize);
        _clip.SetData(data, 0);
    }

    public void OnData(float[] input)
    {
        if (input == null) return;
        int n = input.Length;
        if (_bufferSize + n >= _buffer.Length) return;
        System.Array.Copy(input, 0, _buffer, _bufferSize, n);
        _bufferSize += n;
    }

    public void OnDeviceListUpdated(List<Device> devices)
    {
        this.devices = devices;
    }

    [ContextMenu("Play Toggle")]
    public void TogglePlay()
    {
        if (!audioSource) return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        else
        {
            audioSource.clip = _clip;
            audioSource.Play();
        }
    }

    public IEnumerator STT(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("STT: AudioClip이 비어있습니다.");
            yield break;
        }

        obj_Area_Wait.SetActive(true);

        byte[] wavData = null;
        try
        {
            int length;
            wavData = WavUtility.FromAudioClip(clip, out length);
        }
        catch (Exception e)
        {
            Debug.LogError("STT: WAV 변환 실패 - " + e.Message);
            obj_Area_Wait.SetActive(false);
            yield break;
        }

        if (wavData == null || wavData.Length == 0)
        {
            Debug.LogError("STT: 변환된 오디오 데이터가 없습니다.");
            obj_Area_Wait.SetActive(false);
            yield break;
        }

        // 올바르게 Form 생성
        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "followup.wav", "audio/wav");

        using (UnityWebRequest request = UnityWebRequest.Post(APIConfig.Instance.ClovaSttUrl, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var result = JSON.Parse(request.downloadHandler.text);
                    string resultText = result?["text"];
                    if (!string.IsNullOrEmpty(resultText))
                    {
                        resultText = Regex.Replace(resultText, "<.*?>", string.Empty);
                        stt_Text = resultText;
                        Debug.Log($"?? STT 응답: {resultText} / 개수 {resultText.Length}");
                    }
                    else
                    {
                        Debug.LogWarning("STT: 서버 응답에 text 필드가 없습니다.");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("STT: 응답 파싱 실패 - " + e.Message);
                }
            }
            else
            {
                Debug.LogError($"STT: 요청 실패 - {request.error}");
            }
        }

        // 오디오 메모리 정리
#if UNITY_WEBGL && !UNITY_EDITOR
    if (clip != null)
    {
        clip.UnloadAudioData();
        clip = null;
    }
#else
        if (clip != null)
        {
            Destroy(clip);
            clip = null;
        }
#endif

        obj_Area_Wait.SetActive(false);
    }




}
