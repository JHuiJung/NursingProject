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
using DarkTonic.MasterAudio;

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

    [Header("FeedBack Video"), Space(10)]
    public string video_name = "";
    
    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -20f;
    public float DG_Area_StartY = -450f;
    public Ease DG_Ease = Ease.Linear;

    // �ùķ��̼� �� bool
    bool isSimulationEnd = false;
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

    }

    void CheckSTT_Text()
    {
        string sttText = STT_TTS_Manager.inst.stt_Text;

        if (sttText == "") return;
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

        MasterAudio.PlaySound("Button_Press");

        STT_TTS_Manager.inst.ToggleRecord();
    }

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        isSimulationEnd = true;
        Obj_BTN_Submit.SetActive(false);
        STT_TTS_Manager.inst.stt_Text = string.Empty;
        string answer = txt_VoiceUserInput.text;

        MasterAudio.PlaySound("Button_Press");

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = $"키워드 : {keywords} / 유저의 응답에 핵심 키워드가 포함 되었는지 파악 후 정답, 오답 판별";
        submitForm.txt_userAnswer = answer;
        submitForm.useAiAnswer = true;
        submitForm.video_Name = video_name;
        submitForm.quiz_index = simulation_Quiz_Index;

        _sm.str_Answers.Add(submitForm);

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
}