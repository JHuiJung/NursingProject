using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

[System.Serializable]
public class TextInputForm
{
    public string lable = "";
    public TMP_InputField userInput = null;
    public string correctAnswer = "";
}

public class SimulationTextInput : SimulationBase
{
    [Header("Canvas Obj & Stuff"), SerializeField]
    GameObject Obj_CanvasChoice;
    [SerializeField]
    GameObject Obj_AreaTextInput;
    [SerializeField]
    GameObject Obj_BTN_Submit;


    [TextArea] //질문
    [Header("질문(필수로 입력)")]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    [Header("폼 텍스트 입력")]
    public List<TextInputForm> ls_textInputForm;

    [Header("Dotween"),]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -20f;
    public float DG_Area_StartY = -450f;
    public Ease DG_Ease = Ease.Linear;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnBegin;
    public UnityEngine.Events.UnityEvent OnEnd;

    // 시뮬레이션 끝 bool
    bool isSimulationEnd = false;

    ScenarioManager _sm;

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 시작");

        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup_Normal();

        StartCoroutine(AllUiOn());

        OnBegin?.Invoke(); // 시작 이벤트 호출
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

        //버튼 활성화 or 비활성화
        if(Check_All_InputField_Filled())
        {
            Obj_BTN_Submit.SetActive(true);
        }
        else
        {
            Obj_BTN_Submit.SetActive(false);
        }
    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
        OnEnd?.Invoke(); // 종료 이벤트 호출
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;

        // 모든 입력 필드 비우기
        foreach (var textInputForm in ls_textInputForm)
        {
            textInputForm.userInput.text = string.Empty; // 입력 필드 비우기
        }
    }

    public bool Check_All_InputField_Filled()
    {
        // 모든 입력 필드가 채워졌는지 확인
        foreach (var textInputForm in ls_textInputForm)
        {
            if (string.IsNullOrWhiteSpace(textInputForm.userInput.text))
            {
                return false; // 하나라도 비어있으면 false 반환
            }
        }
        return true; // 모두 채워져 있으면 true 반환
    }

    void Setup_Normal()
    {
        // 질문 텍스트 수정
        Tmp_Question.text = text_Question;
    }

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        isSimulationEnd = true;
        Obj_BTN_Submit.SetActive(false);

        string userAnswer = "";
        foreach (var inputField in ls_textInputForm)
        {
            userAnswer += $"{inputField.lable} : {inputField.userInput.text}";
            userAnswer += "\n";
        }

        string correctAnswer = "";
        foreach (var inputField in ls_textInputForm)
        {
            correctAnswer += $"{inputField.lable} 의 정답 : {inputField.correctAnswer}";
            correctAnswer += "\n";
        }

        // 정답 스택에 추가
        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = correctAnswer;
        submitForm.txt_userAnswer = userAnswer;

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

        // 텍스트 입력 DG
        RectTransform rect_AreaTI = Obj_AreaTextInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x ,DG_Area_EndY), DG_Time
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
        RectTransform rect_AreaTI = Obj_AreaTextInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_StartY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();
    }

}
