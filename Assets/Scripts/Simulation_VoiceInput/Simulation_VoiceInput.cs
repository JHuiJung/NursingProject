using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using uMicrophoneWebGL;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class STTResponse
{
    public string transcript;
    public string feedback;
    public string question;
}

public class Simulation_VoiceInput : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10), SerializeField]
    GameObject Obj_CanvasChoice;
    public GameObject Obj_Area_Wait;
    public MicrophoneWebGL microphoneWebGL;

    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    [Header("보이스 입력"), Space(10)]
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

    //--- 음성 녹음 ----
    private const string apiUrl = "http://127.0.0.1:8000/clova_stt"; // FastAPI /stt 엔드포인트
    private AudioClip recordedClip;
    private bool isRecording = false;
    private const int sampleRate = 16000;
    private const int maxRecordingTime = 30;

    // 시뮬레이션 끝 bool
    bool isSimulationEnd = false;
    private Coroutine autoStopCoroutine;
    ScenarioManager _sm;

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

        Obj_Btn_StartRecord.SetActive(true);
        Obj_Btn_StopRecord.SetActive(false);
    }

    void Setup()
    {
        Tmp_Question.text = text_Question;
    }

    //------------------------------------------------------------------------------------------

    public void StartRecord()
    {
        if (isRecording) return;

        //text 비우기
        txt_VoiceUserInput.text = "";

        //MikeOff 키기
        Obj_Btn_StartRecord.SetActive(false);
        Obj_Btn_StopRecord.SetActive(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        
#else

        recordedClip = Microphone.Start(null, false, maxRecordingTime, sampleRate);
        isRecording = true;

        // 코루틴 실행 후 참조 저장
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
        // 저장된 코루틴이 있다면 중단
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

        string filePath = Path.Combine(Application.persistentDataPath, "recorded.wav");
        SaveWav(filePath, clip);
        byte[] audioData = File.ReadAllBytes(filePath);

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>
        {
            new MultipartFormDataSection("question", question),
            new MultipartFormFileSection("audio", audioData, "recorded.wav", "audio/wav")
        };

        UnityWebRequest request = UnityWebRequest.Post(APIConfig.Instance.ClovaSttUrl, formData);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log("? 응답 수신: " + json);

            STTResponse response = JsonUtility.FromJson<STTResponse>(json);
            txt_VoiceUserInput.text = $"{response.transcript}";

        }
        else
        {
            Debug.LogError("? 전송 실패: " + request.error);
        }

        Obj_Area_Wait.SetActive(false);
    }

    void SaveWav(string path, AudioClip clip)
    {
        var samples = new float[clip.samples];
        clip.GetData(samples, 0);
        byte[] wavData = WavUtility.FromAudioClip(clip, out _, true);
        File.WriteAllBytes(path, wavData);
    }


    // 특정 시간 이후 녹음 종료
    private IEnumerator AutoStopRecordingAfterDelay(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (isRecording) StopRecord();
    }

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        isSimulationEnd = true;

        string answer = txt_VoiceUserInput.text;

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = "미리 제시된 키워드가 있는지 파악 후 정답 검토할 것";
        submitForm.txt_userAnswer = answer;

        _sm.str_Answers.Push(submitForm);

        StartCoroutine(AllUiOff());
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
