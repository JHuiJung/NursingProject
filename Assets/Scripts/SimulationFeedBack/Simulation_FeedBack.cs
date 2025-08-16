using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using static NursingChatClient;


public class Simulation_FeedBack : SimulationBase
{
    public NursingChatClient NursingChatClient;
    public ChatResponse aiResponse = null;

    [Header("Canvas Obj & Stuff"), Space(10),SerializeField]
    GameObject Obj_CanvasChoice;
    [SerializeField]
    GameObject Obj_Title;
    [SerializeField]
    GameObject Obj_Content;
    [SerializeField]
    GameObject Obj_Wait;
    

    [Header("Answer Cards"), Space(10)]
    public GameObject obj_Area_Cards;
    public GameObject obj_Area_BTns;
    public GameObject pf_FeedbackCard;
    public GameObject pf_FeedbackResultCard;
    public List<GameObject> list_FeedbackCards = new List<GameObject>();
    public int currentCardNum = 0;
    [SerializeField]
    TMP_Text txt_PageNum;

    [Header("Pass or NonPass"), Space(10)]
    public float pass_Threshold = 80f;
    public int pass_MoveSimulationIndex = 1;

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_TimeDelta = 0.2f;
    public Ease DG_Ease = Ease.InOutQuad;

    bool isSimulationEnd = false;
    ScenarioManager _sm;
    public override void Enter(ScenarioManager SM)
    {
        _sm = SM;

        Obj_CanvasChoice.SetActive(true);

        StartCoroutine(Setup());
    }

    public override void Excute(ScenarioManager SM)
    {

    }

    public override void Exit(ScenarioManager SM)
    {

        // ȭ�� ����
        Obj_CanvasChoice.SetActive(false);
        StartCoroutine(AllUIOff());
        ResetSimulation();
    }

    public void Pass()
    {
        _sm.NextSimulation();
    }

    public void NonPass()
    {
        _sm.MoveSimulation(pass_MoveSimulationIndex);
    }

    public override void ResetSimulation()
    {
        isSimulationEnd = false;

        // 카드 역순으로 삭제해야 안전
        list_FeedbackCards.Clear();

        for (int i = obj_Area_Cards.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(obj_Area_Cards.transform.GetChild(i).gameObject);
        }

        //%%%%%%%%%%%%%%%%%%%% �ӽ÷� �ǵ�� ����� ���� ���� %%%%%%%%%%%%%%%%%%%%%%
        _sm.str_Answers.Clear();
    }

    IEnumerator Setup()
    {
        Obj_Wait.SetActive(true);
        obj_Area_BTns.SetActive(false);
        Obj_CanvasChoice.SetActive(true);

        string _result = "";

        Stack<SubmitForm> userStack = new Stack<SubmitForm>(_sm.str_Answers.ToArray());
        Stack<SubmitForm> tmpUserStack = new Stack<SubmitForm>(_sm.str_Answers);

        while (userStack.Count > 0)
        {
            SubmitForm back = userStack.Pop();

            _result += "\n";
            _result += $"{back.txt_Question} / QuestionAnswer : {back.txt_QuestionAnswer} / UserAnswer : {back.txt_userAnswer} ";
            _result += "\n--------------------------------";
        }
        // ai를 통해 정보 가져오기
        yield return StartCoroutine(NursingChatClient.SendQuestionToAPIUsing(_result));

        // 답변 리스트 반환
        List<string> qSentences = GetQList(aiResponse.answer);

        // 답변 카드 생성
        DisplayAnswerCards(qSentences, tmpUserStack);

        Obj_Wait.SetActive(false);
        obj_Area_BTns.SetActive(true);

        RectTransform rect_title = Obj_Title.transform.GetComponent<RectTransform>();

        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);
        //------


        yield return new WaitForSeconds(DG_Time);


    }

    public void CardLeftCurl()
    {
        if(currentCardNum - 1 < 0)
        {
            currentCardNum = list_FeedbackCards.Count -1;
        }
        else
        {
            currentCardNum = (currentCardNum - 1) % list_FeedbackCards.Count;
        }

        for (int i = 0; i < list_FeedbackCards.Count; i++)
        {
                if (i == currentCardNum)
                {
                    list_FeedbackCards[i].SetActive(true);
                }
                else
                {
                    list_FeedbackCards[i].SetActive(false);
                }


        }

        txt_PageNum.text = $"{currentCardNum + 1} / {list_FeedbackCards.Count}";
    }

    public void CardRightCurl()
    {

        currentCardNum = (currentCardNum + 1) % list_FeedbackCards.Count;

        for (int i = 0; i < list_FeedbackCards.Count; i++)
        {
            if (i == currentCardNum)
            {
                list_FeedbackCards[i].SetActive(true);
            }
            else
            {
                list_FeedbackCards[i].SetActive(false);
            }


        }

        txt_PageNum.text = $"{currentCardNum + 1} / {list_FeedbackCards.Count}";
    }

    void DisplayAnswerCards(List<string> aiAnswers, Stack<SubmitForm> userAnswersStack)
    {
        List<SubmitForm> userAnswers = new List<SubmitForm>(userAnswersStack);

        print($"{name} : aiAsnwers Cnt :  {aiAnswers.Count} / userAnswer Cnt : {userAnswers.Count}");

        if (aiAnswers.Count != userAnswers.Count)
        {
            print("=== aiAnswers 내용 ===");
            for (int i = 0; i < aiAnswers.Count; i++)
            {
                print($"[{i}] {aiAnswers[i]}");
            }

            print("=== userAnswers 내용 ===");
            for (int i = 0; i < userAnswers.Count; i++)
            {
                print($"[{i}] {userAnswers[i]}");
            }
        }

        // 카드 생성
        for (int i = 0; i < userAnswers.Count; i++)
        {
            string aiAnswer = aiAnswers[i];
            SubmitForm userAnswer = userAnswers[i];

            var np = Instantiate(pf_FeedbackCard, obj_Area_Cards.transform);
            np.name = $"pf_FeedBackCard_{i + 1}";
            FeedBackCard feedBackCard = np.GetComponent<FeedBackCard>();

            feedBackCard.Setup(userAnswer.txt_Question, userAnswer.txt_userAnswer, aiAnswer);

            list_FeedbackCards.Add(np);


        }

        // 결과 카드 추가
        var np2 = Instantiate(pf_FeedbackResultCard, obj_Area_Cards.transform);
        np2.name = $"pf_FeedBackResultCard";
        FeedBackResultCard feedBackResultCard = np2.GetComponent<FeedBackResultCard>();

        feedBackResultCard.Setup(pass_Threshold, aiResponse, pass_MoveSimulationIndex);

        list_FeedbackCards.Add(np2);


        for (int i = 1; i < list_FeedbackCards.Count; i++)
        {
            list_FeedbackCards[i].SetActive(false);
        }

        
        currentCardNum = 0;
        txt_PageNum.text = $"{currentCardNum + 1} / {list_FeedbackCards.Count}";

    }

    List<string> GetQList(string rawText)
    {
        List<string> qSentences = new List<string>();

        // 줄 단위 분리
        string[] lines = rawText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                qSentences.Add(trimmed);
            }
        }

        return qSentences;

        //string pattern = @"Q\d+(?:-\d+)?[:.]\s*((?:오답|정답):\s*.*?)(?=Q\d+(?:-\d+)?[:.]|📊|🎯|$)";
        //MatchCollection matches = Regex.Matches(rawText, pattern, RegexOptions.Singleline);

        //List<string> qSentences = new List<string>();

        //foreach (Match match in matches)
        //{
        //    qSentences.Add(match.Groups[1].Value.Trim());
        //}

        //return qSentences;
    }





    IEnumerator AllUIOn()
    {
        RectTransform rect_title = Obj_Title.transform.parent.GetComponent<RectTransform>();

        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);
    }

    IEnumerator AllUIOff()
    {


        RectTransform rect_title = Obj_Title.transform.GetComponent<RectTransform>();

        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        RectTransform rectContent = Obj_Content.GetComponent<RectTransform>();

        yield return rectContent.DOAnchorPos(new Vector2(0f, -900f), DG_Time).SetEase(DG_Ease).WaitForCompletion();

        yield return new WaitForSeconds(DG_Time);

        Obj_CanvasChoice.SetActive(false);

    }
}
