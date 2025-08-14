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
using static NursingChatClient;

public class Simulation_Conversation : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10), SerializeField]
    GameObject Obj_CanvasChoice;
    public GameObject Obj_Area_Wait;
    public GameObject Obj_Btn_Next;
    public MicrophoneWebGL microphoneWebGL;

    [TextArea] //����
    [Header("질문 (반드시 포함할 것)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;
    public string keywords = "";

    [Header("Voice Input"), Space(10)]
    public TMP_Text txt_VoiceUserInput;
    public GameObject Obj_Area_VoiceInput;
    public GameObject Obj_Btn_StartRecord;
    public GameObject Obj_Btn_StopRecord;
    public GameObject Obj_BTN_Submit;

    [Header("ConvBox"), Space(10)]
    public GameObject Obj_Area_ConvBox;
    public GameObject pf_User_ConvBox;
    public GameObject pf_Opposite_ConvBox;
    public string opposite_Name = "보호자";
    public string opposite_Content = "";

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -20f;
    public float DG_Area_StartY = -450f;
    public Ease DG_Ease = Ease.Linear;

    // �ùķ��̼� �� bool
    bool isSimulationEnd = false;
    private Coroutine autoStopCoroutine;
    ScenarioManager _sm;

    //--- ���� ���� ----
    private bool isRecording = false;
    private const int sampleRate = 16000;
    private const int maxRecordingTime = 30;

    //----WebGl
    [Header("TTS & STT"), Space(10)]
    public AudioSource audioSource;
    public float maxDuration = 10f;
    private float[] _buffer = null;
    private int _bufferSize = 0;
    private AudioClip _clip;
    private bool _isPlaying = false;

    //----ai �亯----
    string aiResponse = "";

    public override void Enter(ScenarioManager SM)
    {
        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup();
        StartCoroutine(AllUiOn());
        StartCoroutine(Start_Simulation());
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

        //��ư Ȱ��ȭ or ��Ȱ��ȭ
        if (string.IsNullOrWhiteSpace(txt_VoiceUserInput.text))
        {
            Obj_BTN_Submit.SetActive(false);
        }
        else
        {
            Obj_BTN_Submit.SetActive(true);
        }
    }

    public override void Exit(ScenarioManager SM)
    {
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;

        txt_VoiceUserInput.text = string.Empty;
        aiResponse = "";

        Obj_Btn_StartRecord.SetActive(true);
        Obj_Btn_StopRecord.SetActive(false);
        Obj_Area_Wait.SetActive(false);

        for (int i = Obj_Area_ConvBox.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(Obj_Area_ConvBox.transform.GetChild(i).gameObject);
        }

    }

    void Setup()
    {
        Tmp_Question.text = text_Question;
        microphoneWebGL.RefreshDeviceList();
    }
    //----- �ùķ��̼� ���� ------

    IEnumerator Start_Simulation()
    {
        // Opposite ConvBox ����
        GameObject oppositeConvbox = Instantiate(pf_Opposite_ConvBox,Obj_Area_ConvBox.transform);
        oppositeConvbox.transform.SetAsLastSibling();
        oppositeConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        oppositeConvbox.GetComponent<ConvBox>().Setup(opposite_Name, opposite_Content);

        // TTS�� ����
        yield return StartCoroutine(PlayTTSQuestion(opposite_Content));

        // convBox ��ĭ �ø���
        yield return StartCoroutine( AllConvBoxMoveUp() );


        // UserConvBox ����
        GameObject userConvbox = Instantiate(pf_User_ConvBox, Obj_Area_ConvBox.transform);
        userConvbox.transform.SetAsLastSibling();
        userConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        userConvbox.GetComponent<ConvBox>().Setup($"{DataManager.inst.userName} 간호사", "");
        txt_VoiceUserInput = userConvbox.GetComponent<ConvBox>().txt_Content;
    }

    IEnumerator End_Simulation()
    {
        // ai ���� �亯 �ޱ�
        Obj_Btn_StartRecord.SetActive(false);
        Obj_Btn_StopRecord.SetActive(false);

        Obj_Area_Wait.SetActive(true);

        yield return StartCoroutine(SendToServer());
        string ai_responese = "";
        if (!string.IsNullOrEmpty(aiResponse))
        {
            ai_responese = aiResponse;
        }
        else
        {
            ai_responese = "ai응답이 없습니다";
        }

        Obj_Area_Wait.SetActive(false);

        // convBox ��ĭ �ø���
        yield return StartCoroutine(AllConvBoxMoveUp());

        // Opposite ConvBox ����
        GameObject oppositeConvbox = Instantiate(pf_Opposite_ConvBox, Obj_Area_ConvBox.transform);
        oppositeConvbox.transform.SetAsLastSibling();
        oppositeConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        oppositeConvbox.GetComponent<ConvBox>().Setup(opposite_Name, ai_responese);

        //tts�� ���
        yield return StartCoroutine(PlayTTSQuestion(ai_responese));

        //
        yield return new WaitForSeconds(3f);

        // ���� ��ư ����
        Obj_Btn_Next.SetActive(true);

        

    }

    public void Next()
    {
        StartCoroutine(AllUiOff());
    }

    IEnumerator AllConvBoxMoveUp()
    {
        int cnt = Obj_Area_ConvBox.transform.childCount;
        float timeOffset = 0.5f;

        for (int i = 0; i < Obj_Area_ConvBox.transform.childCount; i++)
        {
            ConvBox convBox = Obj_Area_ConvBox.transform.GetChild(i).GetComponent<ConvBox>();

            convBox.MoveUp(timeOffset);
        }

        yield return new WaitForSeconds(timeOffset * cnt);
    }

    //------------------------------------------------------------------------------------------


    #region ----------------------------------------STT 
    public void ToggleRecord()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        if (!microphoneWebGL || !microphoneWebGL.isValid) return;

        bool isMRecording = microphoneWebGL.isRecording;

        if (!isMRecording)
        {
            Begin();
        }
        else
        {
            End();
        }

        isMRecording = !isMRecording;
#else
        if (isRecording)
        {
            End();
        }
        else
        {
            Begin();
        }
#endif


    }

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

    private void Begin()
    {
        //text ����
        txt_VoiceUserInput.text = "";



#if UNITY_WEBGL && !UNITY_EDITOR
        Obj_Btn_StartRecord.SetActive(false);
        Obj_Btn_StopRecord.SetActive(true);
        microphoneWebGL.Begin();
#else
        if (isRecording) return;

        Obj_Btn_StartRecord.SetActive(false);
        Obj_Btn_StopRecord.SetActive(true);

        _clip = Microphone.Start(null, false, maxRecordingTime, sampleRate);
        isRecording = true;
        // �ڷ�ƾ ���� �� ���� ����
        print("Begin");
#endif
    }

    private void End()
    {

        print("End");

#if UNITY_WEBGL && !UNITY_EDITOR
        Obj_Btn_StartRecord.SetActive(true);
        Obj_Btn_StopRecord.SetActive(false);
        microphoneWebGL.End();
        StartCoroutine(SendWavToServer(_clip, text_Question));
#else

        if (!isRecording) return;

        Obj_Btn_StartRecord.SetActive(true);
        Obj_Btn_StopRecord.SetActive(false);

        Microphone.End(null);
        isRecording = false;
        // ����� �ڷ�ƾ�� �ִٸ� �ߴ�
        if (autoStopCoroutine != null)
        {
            StopCoroutine(autoStopCoroutine);
            autoStopCoroutine = null;
        }
        StartCoroutine(SendWavToServer(_clip, text_Question));
#endif
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

    #endregion

    IEnumerator SendWavToServer(AudioClip clip, string question)
    {
        Obj_Area_Wait.SetActive(true);

        int length;
        byte[] wavData = WavUtility.FromAudioClip(clip, out length);

        WWWForm form = new WWWForm();
        form.AddBinaryData("audio", wavData, "followup.wav", "audio/wav");

        string url = APIConfig.Instance.ClovaSttUrl;

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JSON.Parse(request.downloadHandler.text);
            string resultText = result["text"];
            txt_VoiceUserInput.text = resultText;
            Debug.Log("? 응답: " + resultText);
        }
        else
        {
            Debug.LogError("? 응답 오류: " + request.error);
        }

        Obj_Area_Wait.SetActive(false);
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

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL 환경: base64 → AudioClip 직접 생성 및 재생
            AudioClip clip = WavToAudioClip(audioBytes, "TTS_AudioClip");
            PlayClip(clip);
#else
            // 에디터/PC 환경: 파일 저장 후 재생
            PlayAudioFromBytes(audioBytes);
#endif
        }
        else
        {
            Debug.LogError("TTS 응답 오류: " + request.error);
        }
    }
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



    void PlayClip(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
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
                Debug.LogError("TTS 재생 오류: " + www.error);
            }
        }
    }

    //���� �� UI

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        isSimulationEnd = true;
        Obj_BTN_Submit.SetActive(false);

        string answer = txt_VoiceUserInput.text;

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = $"상대방 질문 : {opposite_Content} / 유저의 답변에 포함되야할 키워드 : {keywords} " +
            $" / 상대방의 질문과 키워드를 참고해서 정답, 오답 판별";
        submitForm.txt_userAnswer = answer;

        _sm.str_Answers.Push(submitForm);

        StartCoroutine(End_Simulation());
    }

    public IEnumerator SendToServer()
    {

        string userRes = txt_VoiceUserInput.text;

        WWWForm form = new WWWForm();
        form.AddField("parent_question", opposite_Content);
        form.AddField("user_response", userRes);
        form.AddField("keywords", keywords);

        string url = APIConfig.Instance.ParentResponse;

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JSON.Parse(request.downloadHandler.text);
            string followupText = result["parent_response"];
            aiResponse = followupText;
            Debug.Log("? 응답: " + followupText);
        }
        else
        {
            Debug.LogError("? 응답 오류: " + request.error);
        }
    }

    IEnumerator AllUiOn()
    {
        // Ÿ��Ʋ DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // ���̽� �Է� DG
        RectTransform rect_AreaTI = Obj_Area_VoiceInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_EndY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);
    }

    IEnumerator AllUiOff()
    {
        // Ÿ��Ʋ DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);

        // ���� �ùķ��̼����� �̵�
        _sm.NextSimulation();


    }
}
