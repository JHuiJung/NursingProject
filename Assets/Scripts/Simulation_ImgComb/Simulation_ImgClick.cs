using DarkTonic.MasterAudio;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Simulation_ImgClick : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10)]
    public GameObject Obj_CanvasChoice;
    public GameObject Obj_SubmitButton;

    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    [Header("피드백 설정"), Space(10)]
    public string video_Name = "";
    public bool useAiAnswer = true; // AI 답변 사용 여부

    [Header("카드 정보"), Space(10)]
    public List<ImgClick_Entity> imgClick_Entities = new List<ImgClick_Entity>();
    public List<Vector2> cardsPositions = new List<Vector2>();
    public GameObject Obj_Warning;
    public int currentCardNumber = 0;

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public Ease DG_Ease = Ease.InOutQuad;
    public float DG_deltaTime = 0.15f;

    

    // 시뮬레이션 끝 bool
    private bool isSimulationEnd = false;
    private ScenarioManager _sm;


    string userAnswer = "";

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 시작");

        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup();
        StartCoroutine(AllUiOn());
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;


        if(IsAllClicked())
        {
            //// 현재 imgClick_entity의 숫자가 겹치는게 있는지 확인
            //for (int i = 0; i < imgClick_Entities.Count; i++)
            //{
            //    for (int j = i + 1; j < imgClick_Entities.Count; j++)
            //    {
            //        if (imgClick_Entities[i].number == imgClick_Entities[j].number)
            //        {
            //            Obj_Warning.SetActive(true);
            //            Obj_SubmitButton.SetActive(false);
            //            return;
            //        }
            //    }
            //}

            //Obj_Warning.SetActive(false);
            Obj_SubmitButton.SetActive(true);
        }
        else
        {
            Obj_Warning.SetActive(false);
            Obj_SubmitButton.SetActive(false);

        }


    }

    bool IsAllClicked()
    {
        foreach (var item in imgClick_Entities)
        {
            if (!item.isClicked)
            {
                return false;
            }
        }
        return true;
    }

    public void CardOrganise()
    {

        int num = 1;

        imgClick_Entities.Sort((a, b) =>
        {
            int numA = a.clickedOrder;
            int numB = b.clickedOrder;
            return numA.CompareTo(numB);
        });

        for (int i = 0; i < imgClick_Entities.Count; i++)
        {
            if (!imgClick_Entities[i].isClicked) continue;
            imgClick_Entities[i].number = num;
            imgClick_Entities[i].txt_Num.text = num.ToString();
            num++;
        }


    }

    public void Submit()
    {
        isSimulationEnd = true;

        Obj_SubmitButton.SetActive(false);

        imgClick_Entities.Sort((a, b) =>
        {
            int numA = a.number;
            int numB = b.number;
            return numA.CompareTo(numB);
        });


        for (int i = 0; i < imgClick_Entities.Count; i++)
        {

            ImgClick_Entity e = imgClick_Entities[i];

            if (!e.isClicked) continue;

            userAnswer += $"[{e.number}번 : {e.txt_Title.text}]";

            if (i != imgClick_Entities.Count - 1)
            {
                userAnswer += " -> ";
            }


        }

        MasterAudio.PlaySound("Button_Press");

        print($"{name} : {userAnswer}");

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = "미리 제공된 답변 참고";
        submitForm.txt_userAnswer = userAnswer;
        submitForm.video_Name = video_Name;
        submitForm.useAiAnswer = useAiAnswer; // AI 답변 사용 여부
        submitForm.quiz_index = simulation_Quiz_Index;

        _sm.str_Answers.Add(submitForm);

        StartCoroutine(AllUiOff());

    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);

    }
    public override void ResetSimulation()
    {
        userAnswer = "";
        isSimulationEnd = false;

        Obj_Warning.SetActive(false);

        foreach (var item in imgClick_Entities)
        {
            item.CardReset();
        }
    }

    //------------------------------------------------------------------------------------------

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // 엔티티 카드 섞기
        for (int i = 0; i < imgClick_Entities.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, imgClick_Entities.Count);
            ImgClick_Entity temp = imgClick_Entities[i];
            imgClick_Entities[i] = imgClick_Entities[randomIndex];
            imgClick_Entities[randomIndex] = temp;
        }

        // 엔티티 카드 닷트윈
        for (int i = 0; i < imgClick_Entities.Count; i++)
        {
            imgClick_Entities[i].GetComponent<RectTransform>().DOAnchorPos(
                cardsPositions[i], DG_deltaTime * (i + 1)).SetEase(DG_Ease);
        }

        yield return new WaitForSeconds(DG_deltaTime * imgClick_Entities.Count);
    }

    IEnumerator AllUiOff()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        // 엔티티 카드 닷트윈
        for (int i = 0; i < imgClick_Entities.Count; i++)
        {
            imgClick_Entities[i].GetComponent<RectTransform>().DOAnchorPos(
                new Vector2(0, 900f), DG_deltaTime * (i + 1)).SetEase(DG_Ease);
        }


        yield return new WaitForSeconds(DG_deltaTime * imgClick_Entities.Count);

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();
    }

    void Setup()
    {
        // 질문 텍스트 수정
        Tmp_Question.text = text_Question;


    }
}
