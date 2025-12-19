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
    public bool submitUseAiAnswer = true;

    [Header("Voice Input"), Space(10)]
    public TMP_InputField inputField_VoiceUserInput;
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

    bool flag_Input = false;


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
        flag_Input = false;

        inputField_VoiceUserInput.text = "";

        Obj_Btn_StartRecord.SetActive(true);
        Obj_Btn_StopRecord.SetActive(false);

        _sm.GageObjSetActive(false);
    }

    void Setup()
    {
        Tmp_Question.text = text_Question;
        STT_TTS_Manager.inst.stt_Text = "";
        _sm.GageObjSetActive(true);
        _sm.GageUpdate();
    }

    void CheckSTT_Text()
    {

        if (!flag_Input)
        {
            // 텍스트 인풋 클릭 안했을때
            string sttText = STT_TTS_Manager.inst.stt_Text;

            if (sttText == "") return;
            inputField_VoiceUserInput.text = sttText;
            Obj_BTN_Submit.SetActive(true);
        }
        else
        {
            string _text = inputField_VoiceUserInput.text;

            if (_text == "") 
            {
                Obj_BTN_Submit.SetActive(false);
            }
            else
            {
                Obj_BTN_Submit.SetActive(true);
            }
        }
        
    }

    public void ToggleRecord()
    {

        bool isMrocording = STT_TTS_Manager.inst.isMRecording;

        if (!isMrocording)
        {
            // recording - begin
            inputField_VoiceUserInput.text = "";
            STT_TTS_Manager.inst.stt_Text = "";

            flag_Input = false;

            Obj_Btn_StartRecord.SetActive(false);
            Obj_Btn_StopRecord.SetActive(true);

        }
        else
        {
            // no Recording - end

            inputField_VoiceUserInput.text = "";
            STT_TTS_Manager.inst.stt_Text = "";

            flag_Input = false;

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
        string answer = inputField_VoiceUserInput.text;

        MasterAudio.PlaySound("Button_Press");

        if(submitUseAiAnswer)
        {
            SubmitForm submitForm = new SubmitForm();
            submitForm.txt_Question = text_Question;
            submitForm.txt_QuestionAnswer = $"키워드 : {keywords} / 유저의 응답에 핵심 키워드 중 하나라도 포함 되었는지 파악 후 정답, 오답 판별후, 어떤 키워드가 포함되어있어야 하는지 함께 답변";
            submitForm.txt_userAnswer = answer;
            submitForm.useAiAnswer = true;
            submitForm.video_Name = video_name;
            submitForm.quiz_index = simulation_Quiz_Index;

            _sm.str_Answers.Add(submitForm);

            DataManager.Conv_Log_Unit convUnit = new DataManager.Conv_Log_Unit();
            DataManager.Conv_Log conv_Log = new DataManager.Conv_Log();

            convUnit.log = answer;
            convUnit.isPlayer = "O";
            convUnit.name_conv = DataManager.inst.userName;

            conv_Log.question = text_Question;
            conv_Log.ls_Conv_Log_Unit.Add(convUnit);

            DataManager.inst.AddConvLogList(conv_Log);
        }

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

    public void Flag_Input(bool flag)
    {
        flag_Input = flag;
    }
}