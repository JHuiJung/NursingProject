using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Simulation_Paper : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10)]
    public GameObject Obj_CanvasChoice;
    public GameObject Obj_Area_TextInput;
    public GameObject Obj_BTN_Submit;

    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;
    public string keywords = "";

    [Header("Txts"), Space(10)]
    public TMP_Text txt_Date;
    public TMP_Text txt_Time;
    public TMP_Text txt_Writer;

    [Header("InputField"), Space(10)]
    public List<TMP_InputField> InputFields = new List<TMP_InputField>();

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -10f;
    public float DG_Area_StartY = -850f;
    public Ease DG_Ease = Ease.InOutQuad;

    public bool isSimulationEnd = false;
    private ScenarioManager _sm;

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

        if(isFillAnyText())
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
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;
    }

    public void Submit()
    {
        string userAnswer = "";

        for (int i = 0; i < InputFields.Count; i++)
        {

            if (!string.IsNullOrEmpty(InputFields[i].text))
            {
                userAnswer += InputFields[i].text;
                userAnswer += "\n";
            }
        }

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = $" 환자 정보 : [{DataManager.inst.patient_Information}] / 핵심 키워드 : {keywords} / 핵심 키워드와 환자 정보";
        submitForm.txt_userAnswer = userAnswer;
        _sm.str_Answers.Push(submitForm);

        print($"{submitForm.txt_Question} / {submitForm.txt_QuestionAnswer} / {submitForm.txt_userAnswer}");
        StartCoroutine(AllUiOff());
    }

    bool isFillAnyText()
    {
        for (int i = 0; i < InputFields.Count; i++)
        {
            
            if(!string.IsNullOrEmpty(InputFields[i].text))
            {
                return true;
            }
        }
        return false;
    }

    //------------------------------------------------------------------------------------------

    void Setup()
    {
        // 질문 텍스트 수정
        Tmp_Question.text = text_Question;

        txt_Date.text = DateTime.Now.ToString("yyyy.MM.dd");
        txt_Time.text = _sm.inGameTime;
        txt_Writer.text = DataManager.inst.userName;

    }

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        RectTransform rect_AreaTI = Obj_Area_TextInput.GetComponent<RectTransform>();

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

        RectTransform rect_AreaTI = Obj_Area_TextInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_StartY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();
    }


    public virtual void SubmitAnswer(string answer, int choosedNum)
    {
        isSimulationEnd = true;

        

        //_sm.str_Answers.Push($"{text_Question} / User Answer : {answer}");


        // DG UI OFF
        StartCoroutine(AllUiOff());
    }

}
