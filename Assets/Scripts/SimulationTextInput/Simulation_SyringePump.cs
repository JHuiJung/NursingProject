using DarkTonic.MasterAudio;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Simulation_SyringePump : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10), SerializeField]
    GameObject Obj_CanvasChoice;
    [SerializeField]
    GameObject Obj_BTN_Submit;
    public GameObject Obj_Area_SyringPump;

    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    [Header("Syringe Pump Info"), Space(10)]
    public TMP_Text txt_SyringePump_amount;
    public float user_Answer = 20f;
    public float Question_Answer = 20f;
    public string Numeric_Unit = "ml/hr";

    [Header("Feedback")]
    public string video_name = "";
    public Sprite img_Sprite = null;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnBegin;
    public UnityEngine.Events.UnityEvent OnEnd;
    public UnityEngine.Events.UnityEvent OnNonPassEnd;

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -50f;
    public float DG_Area_StartY = -800f;
    public Ease DG_Ease = Ease.InOutQuad;

    // 시뮬레이션 끝 bool
    bool isSimulationEnd = false;
    bool isPass = false;

    ScenarioManager _sm;

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 문제 시작");

        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup();

        StartCoroutine(AllUiOn());

        OnBegin?.Invoke(); // 시작 이벤트 호출
        if(!isPass)
        {
            OnNonPassEnd?.Invoke();
        }
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
        OnEnd?.Invoke(); // 종료 이벤트 호출
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;
        Obj_BTN_Submit.SetActive(false);
        _sm.GageObjSetActive(false);
    }

    //------------------------------------------------------------------------------------------

    void Setup()
    {
        // 질문 텍스트 수정
        Tmp_Question.text = text_Question;
        txt_SyringePump_amount.text = $"{user_Answer}{Numeric_Unit}";
        isPass = false;

        Obj_BTN_Submit.SetActive(true);
        _sm.GageObjSetActive(true);
        _sm.GageUpdate();
    }

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        MasterAudio.PlaySound("Button_Press");

        isSimulationEnd = true;
        Obj_BTN_Submit.SetActive(false);

        string answer = $"{user_Answer:F1}{Numeric_Unit}";
        string qustion_Answer = $"{Question_Answer}{Numeric_Unit}";

        isPass = (answer == qustion_Answer);

        // 정답 스택에 추가
        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = qustion_Answer;
        submitForm.txt_userAnswer = answer;
        submitForm.quiz_index = simulation_Quiz_Index;
        submitForm.video_Name = video_name;
        submitForm.useAiAnswer = false;
        if(img_Sprite != null)
            submitForm.img_Sprite = img_Sprite;

        _sm.str_Answers.Add(submitForm);

        print($"{name} : UserAnswer -> {answer} / Question Answer -> {Question_Answer}{Numeric_Unit}");

        StartCoroutine(AllUiOff());
    }

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // 텍스트 입력 DG
        RectTransform rect_AreaTI = Obj_Area_SyringPump.GetComponent<RectTransform>();

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

        // 텍스트 입력 DG
        RectTransform rect_AreaTI = Obj_Area_SyringPump.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_StartY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();
    }

    public void ValueChange(float value)
    {
        MasterAudio.PlaySound("Button_Press");
        user_Answer += value;
        txt_SyringePump_amount.text = $"{user_Answer:F1}{Numeric_Unit}";
    }
}
