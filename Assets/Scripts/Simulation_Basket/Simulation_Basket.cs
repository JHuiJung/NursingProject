using DarkTonic.MasterAudio;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Simulation_Basket : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10)]
    public GameObject Obj_CanvasChoice;
    public GameObject Obj_BTN_Submt;
    public GameObject Obj_Area_Basket;
    public List<GameObject> Obj_BasketEntites = new List<GameObject>();

    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    [Header("- Basket -"), Space(10)]
    public float findRangeY = 400;

    [Header("FeedBack Video"), Space(10)]
    public string video_name = "";

    [Header("- Dotween -"), Space(10)]
    public float DG_Time = 0.75f;
    public Ease DG_Ease = Ease.InOutQuad;
    public float DG_deltaTime = 0.15f;

    // 시뮬레이션 끝 bool
    public bool isSimulationEnd = false;
    private ScenarioManager _sm;

    //유저 정답
    string userAnswer = "";

    public override void Enter(ScenarioManager SM)
    {
        _sm = SM;
        Setup();
        Obj_CanvasChoice.SetActive(true);
        StartCoroutine(AllUiOn());
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

        Check();

        if(isEntityOn())
        {
            Obj_BTN_Submt.SetActive(true);
        }
        else
        {
            Obj_BTN_Submt.SetActive(false);
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

        userAnswer = "";

        // 엔티티 원위치
        for (int i = 0; i < Obj_BasketEntites.Count; i++)
        {
            RectTransform rect_BE = Obj_BasketEntites[i].GetComponent<RectTransform>();

            Vector2 pos = new Vector2(0, 800);

            rect_BE.anchoredPosition = pos;
        }
    }

    void Check()
    {
        if (isSimulationEnd) return;

        // 체크 표시
        foreach(GameObject be in Obj_BasketEntites)
        {
            float beY = be.GetComponent<RectTransform>().anchoredPosition.y;

            if(beY <= findRangeY)
            {
                be.GetComponent<BasketEntity>().OnOffCheck(true);
            }
            else
            {
                be.GetComponent<BasketEntity>().OnOffCheck(false);
            }
        }

    }

    // 한개라도 entity가 on이면 true
    bool isEntityOn()
    {

        foreach (GameObject be in Obj_BasketEntites)
        {
           if( be.GetComponent<BasketEntity>().isCheck )
                return true;

        }

        return false;
    }

    void Setup()
    {
        Tmp_Question.text = text_Question;
    }

    public void Submit()
    {
        if (isSimulationEnd) return;
        isSimulationEnd = true;

        Obj_BTN_Submt.SetActive(false);

        // check 된 엔티티 만 답에 추가

        userAnswer += "담은 물품 [ ";

        foreach (GameObject be in Obj_BasketEntites)
        { 
            BasketEntity bee = be.GetComponent<BasketEntity>();

            if (bee.isCheck)
            {
                userAnswer += $"{bee.entity_Title} ,";
            }

        }

        userAnswer += " ] ";

        MasterAudio.PlaySound("Button_Press");

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = "미리 제공된 답변 참고";
        submitForm.txt_userAnswer = userAnswer;
        submitForm.useAiAnswer = true; // AI 답변 사용 여부
        submitForm.quiz_index = simulation_Quiz_Index;
        submitForm.video_Name = video_name;
        _sm.str_Answers.Add(submitForm);

        StartCoroutine(AllUiOff());

    }

    //-----------------

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // Basket DG
        RectTransform rect_Basket = Obj_Area_Basket.GetComponent<RectTransform>();

        rect_Basket.DOAnchorPos(new Vector2(rect_Basket.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // Basket Entities DG
        for (int i = 0; i < Obj_BasketEntites.Count; i++)
        {
            RectTransform rect_BE = Obj_BasketEntites[i].GetComponent<RectTransform>();

            Vector2 endPos = new Vector2(Random.Range(-700f, 700), Random.Range(75f, 200f));

            rect_BE.DOAnchorPos(endPos, DG_deltaTime * (i + 1)).SetEase(DG_Ease);
        }

        yield return new WaitForSeconds(Obj_BasketEntites.Count * DG_deltaTime);
    }

    IEnumerator AllUiOff()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        // Basket DG
        RectTransform rect_Basket = Obj_Area_Basket.GetComponent<RectTransform>();

        rect_Basket.DOAnchorPos(new Vector2(rect_Basket.anchoredPosition.x,
            -500f), DG_Time).SetEase(DG_Ease);

        // Basket Entities DG
        for (int i = 0; i < Obj_BasketEntites.Count; i++)
        {
            RectTransform rect_BE = Obj_BasketEntites[i].GetComponent<RectTransform>();

            Vector2 endPos = new Vector2(0,-800);

            rect_BE.DOAnchorPos(endPos, DG_deltaTime * (i + 1)).SetEase(DG_Ease);
        }
        yield return new WaitForSeconds(Obj_BasketEntites.Count * DG_deltaTime);

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();
    }

}
