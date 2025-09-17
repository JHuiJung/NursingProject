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

public class Simulation_VoiceInput : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10), SerializeField]
    GameObject Obj_CanvasChoice;
    public GameObject Obj_Area_Wait;
    //public MicrophoneWebGL microphoneWebGL;

    [TextArea] //����
    [Header("질문 (반드시 포함할 것)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;
    [TextArea]
    public string keywords = "";

    [Header("Voice Input"), Space(10)]
    public TMP_Text txt_VoiceUserInput;
    public GameObject Obj_Area_VoiceInput;
    public GameObject Obj_Btn_StartRecord;
    public GameObject Obj_Btn_StopRecord;
    public GameObject Obj_BTN_Submit;

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -20f;
    public float DG_Area_StartY = -450f;
    public Ease DG_Ease = Ease.Linear;

    ////--- ���� ���� ----
    //private AudioClip recordedClip;
    //private bool isRecording = false;
    //private const int sampleRate = 16000;
    //private const int maxRecordingTime = 30;

    ////----WebGl
    //public AudioSource audioSource;
    //public float maxDuration = 10f;
    //private float[] _buffer = null;
    //private int _bufferSize = 0;
    //private AudioClip _clip;
    //private bool _isPlaying = false;

    // �ùķ��̼� �� bool
    bool isSimulationEnd = false;
    ScenarioManager _sm;
    //private Coroutine autoStopCoroutine;


    public override void Enter(ScenarioManager SM)
    {
        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup();
        StartCoroutine(AllUiOn());
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

        // sumbit button active

        CheckSTT_Text();
    }

    public override void Exit(ScenarioManager SM)
    {
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;

        txt_VoiceUserInput.text = "";

        Obj_Btn_StartRecord.SetActive(true);
        Obj_Btn_StopRecord.SetActive(false);
    }

    void Setup()
    {
        Tmp_Question.text = text_Question;

        // --- 텍스트 입력하는곳 다시 생성
        //GameObject newInputField = Instantiate(txt_VoiceUserInput.gameObject, txt_VoiceUserInput.transform.parent);
        //Destroy(txt_VoiceUserInput.gameObject);
        //txt_VoiceUserInput = newInputField.GetComponent<TMP_Text>();

    }

    void CheckSTT_Text()
    {
        string sttText = STT_TTS_Manager.inst.stt_Text;

        if (sttText == "") return;

        // set userbox text
        //Debug.Log($"?? STT 응답: {sttText} / 개수 {sttText.Length}");
        txt_VoiceUserInput.text = sttText;
        Obj_BTN_Submit.SetActive(true);
    }

    public void ToggleRecord()
    {

        bool isMrocording = STT_TTS_Manager.inst.isMRecording;

        if (!isMrocording)
        {
            // recording - begin
            txt_VoiceUserInput.text = "";

            Obj_Btn_StartRecord.SetActive(false);
            Obj_Btn_StopRecord.SetActive(true);

        }
        else
        {
            // no Recording - end

            Obj_Btn_StartRecord.SetActive(true);
            Obj_Btn_StopRecord.SetActive(false);
        }

        STT_TTS_Manager.inst.ToggleRecord();
    }

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        isSimulationEnd = true;
        Obj_BTN_Submit.SetActive(false);
        STT_TTS_Manager.inst.stt_Text = string.Empty;
        string answer = txt_VoiceUserInput.text;

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = $"키워드 : {keywords} / 유저의 응답에 핵심 키워드가 포함 되었는지 파악 후 정답, 오답 판별";
        submitForm.txt_userAnswer = answer;

        _sm.str_Answers.Push(submitForm);

        StartCoroutine(AllUiOff());
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

        // ���̽� �Է� DG
        RectTransform rect_AreaTI = Obj_Area_VoiceInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_StartY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);

        // ���� �ùķ��̼����� �̵�
        _sm.NextSimulation();
    }

    /*

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
        if(isRecording)
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

        recordedClip = Microphone.Start(null, false, maxRecordingTime, sampleRate);
        isRecording = true;
        // �ڷ�ƾ ���� �� ���� ����
        print("Begin");

        autoStopCoroutine = StartCoroutine(AutoStopRecordingAfterDelay(maxRecordingTime));
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
        StartCoroutine(SendWavToServer(recordedClip, text_Question));
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


    public void StartRecord()
    {
        if (isRecording) return;

        //text ����
        txt_VoiceUserInput.text = "";

        //MikeOff Ű��
        Obj_Btn_StartRecord.SetActive(false);
        Obj_Btn_StopRecord.SetActive(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        
#else

        recordedClip = Microphone.Start(null, false, maxRecordingTime, sampleRate);
        isRecording = true;

        // �ڷ�ƾ ���� �� ���� ����
        autoStopCoroutine = StartCoroutine(AutoStopRecordingAfterDelay(maxRecordingTime));
#endif
    }

    public void StopRecord()
    {
        if (!isRecording) return;

        Obj_Btn_StartRecord.SetActive(true);
        Obj_Btn_StopRecord.SetActive(false);

#if UNITY_WEBGL && !UNITY_EDITOR
        
#else
        Microphone.End(null);
        isRecording = false;
        // ����� �ڷ�ƾ�� �ִٸ� �ߴ�
        if (autoStopCoroutine != null)
        {
            StopCoroutine(autoStopCoroutine);
            autoStopCoroutine = null;
        }
        StartCoroutine(SendWavToServer(recordedClip, text_Question));
#endif
    }

    IEnumerator SendWavToServer(AudioClip clip, string question)
    {
        Obj_Area_Wait.SetActive(true);

        int length;
        
        // 🎙️ WebGL에서는 16kHz 리샘플링 사용 (STT 최적화)
#if UNITY_WEBGL && !UNITY_EDITOR
        byte[] wavData = WavUtility.FromAudioClipResample16kHz(clip, out length, true);
        Debug.Log($"🎙️ WebGL STT: {clip.frequency}Hz → 16000Hz 리샘플링 완료");
#else
        byte[] wavData = WavUtility.FromAudioClip(clip, out length);
#endif

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


    // Ư�� �ð� ���� ���� ����
    private IEnumerator AutoStopRecordingAfterDelay(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (isRecording) StopRecord();
    }
    */






}