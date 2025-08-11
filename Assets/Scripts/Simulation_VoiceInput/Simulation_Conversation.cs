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

    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;
    public string keywords = "";

    [Header("보이스 입력"), Space(10)]
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

    // 시뮬레이션 끝 bool
    bool isSimulationEnd = false;
    private Coroutine autoStopCoroutine;
    ScenarioManager _sm;

    //--- 음성 녹음 ----
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

    //----ai 답변----
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

        //버튼 활성화 or 비활성화
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
        print($"{name} : 객관식 문제 끝");
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

        while(Obj_Area_ConvBox.transform.childCount > 0)
        {
            Destroy(Obj_Area_ConvBox.transform.GetChild(0).gameObject);
        }
    }

    void Setup()
    {
        Tmp_Question.text = text_Question;
    }
    //----- 시뮬레이션 조작 ------

    IEnumerator Start_Simulation()
    {
        // Opposite ConvBox 생성
        GameObject oppositeConvbox = Instantiate(pf_Opposite_ConvBox,Obj_Area_ConvBox.transform);
        oppositeConvbox.transform.SetAsLastSibling();
        oppositeConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        oppositeConvbox.GetComponent<ConvBox>().Setup(opposite_Name, opposite_Content);

        print("TTS 전");
        // TTS로 질문
        yield return StartCoroutine(PlayTTSQuestion(opposite_Content));
        print("TTS 후");

        // convBox 한칸 올리기
        yield return StartCoroutine( AllConvBoxMoveUp() );


        // UserConvBox 생성
        GameObject userConvbox = Instantiate(pf_User_ConvBox, Obj_Area_ConvBox.transform);
        userConvbox.transform.SetAsLastSibling();
        userConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        userConvbox.GetComponent<ConvBox>().Setup($"{DataManager.inst.userName} 간호사", "");
        txt_VoiceUserInput = userConvbox.GetComponent<ConvBox>().txt_Content;
    }

    IEnumerator End_Simulation()
    {
        // ai 한테 답변 받기
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
            ai_responese = "ai로 부터 답변을 받지 못했음";
        }

        Obj_Area_Wait.SetActive(false);

        // convBox 한칸 올리기
        yield return StartCoroutine(AllConvBoxMoveUp());

        // Opposite ConvBox 생성
        GameObject oppositeConvbox = Instantiate(pf_Opposite_ConvBox, Obj_Area_ConvBox.transform);
        oppositeConvbox.transform.SetAsLastSibling();
        oppositeConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        oppositeConvbox.GetComponent<ConvBox>().Setup(opposite_Name, ai_responese);

        //tts로 대답
        yield return StartCoroutine(PlayTTSQuestion(ai_responese));

        //
        yield return new WaitForSeconds(3f);

        // 다음 버튼 생성
        Obj_Btn_Next.SetActive(true);

        

    }

    public void NextSimulation()
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


    #region ----------------------------------------STT 구현
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
        //text 비우기
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
        // 코루틴 실행 후 참조 저장
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
        // 저장된 코루틴이 있다면 중단
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
            Debug.Log("? 후속 답변 저장 완료: " + resultText);
        }
        else
        {
            Debug.LogError("? 후속 답변 서버 전송 실패: " + request.error);
        }

        //string filePath = Path.Combine(Application.persistentDataPath, "recorded.wav");
        //SaveWav(filePath, clip);
        //byte[] audioData = File.ReadAllBytes(filePath);

        //List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        //{
        //    new MultipartFormDataSection("question", question),
        //    new MultipartFormFileSection("audio", audioData, "recorded.wav", "audio/wav")
        //};

        //UnityWebRequest request = UnityWebRequest.Post(APIConfig.Instance.ClovaSttUrl, formData);
        //request.downloadHandler = new DownloadHandlerBuffer();

        //yield return request.SendWebRequest();

        //if (request.result == UnityWebRequest.Result.Success)
        //{
        //    string json = request.downloadHandler.text;
        //    Debug.Log("? 응답 수신: " + json);

        //    STTResponse response = JsonUtility.FromJson<STTResponse>(json);
        //    txt_VoiceUserInput.text = $"{response.transcript}";

        //}
        //else
        //{
        //    Debug.LogError("? 전송 실패: " + request.error);
        //}

        Obj_Area_Wait.SetActive(false);
    }

    void SaveWav(string path, AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);
        byte[] wavData = WavUtility.FromAudioClip(clip, out _, true);
        File.WriteAllBytes(path, wavData);
    }


    //---------- TTS ----------------
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
            Debug.LogError("? 질문 TTS 요청 실패: " + request.error);
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
                Debug.LogError("? TTS 오디오 로드 실패: " + www.error);
            }
        }
    }

    //제출 및 UI

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        isSimulationEnd = true;
        Obj_BTN_Submit.SetActive(false);

        string answer = txt_VoiceUserInput.text;

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = $"상대방 질문 : {opposite_Content} / 유저의 답변에 포함되어야 하는 키워드 {keywords} " +
            $" / 키워드들이 포함되었는지 검토 후 정답 오답 판단할 것";
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
            Debug.Log("? 후속 답변 저장 완료: " + followupText);
        }
        else
        {
            Debug.LogError("? 후속 답변 서버 전송 실패: " + request.error);
        }
    }

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // 보이스 입력 DG
        RectTransform rect_AreaTI = Obj_Area_VoiceInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_EndY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);
    }

    IEnumerator AllUiOff()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        // 보이스 입력 DG
        RectTransform rect_AreaTI = Obj_Area_VoiceInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_StartY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();


    }
}
