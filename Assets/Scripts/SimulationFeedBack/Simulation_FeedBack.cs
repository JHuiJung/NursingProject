using DG.Tweening;
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
    public GameObject pf_FeedbackCard;
    public List<GameObject> list_FeedbackCards = new List<GameObject>();


    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
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
        ResetSimulation();

        StartCoroutine(AllUIOff());

        //%%%%%%%%%%%%%%%%%%%% �ӽ÷� �ǵ�� ����� ���� ���� %%%%%%%%%%%%%%%%%%%%%%
        _sm.str_Answers.Clear();
    }

    

    public override void ResetSimulation()
    {
        isSimulationEnd = false;
    }

    IEnumerator Setup()
    {
        Obj_Wait.SetActive(true);
        Obj_CanvasChoice.SetActive(true);

        string _result = "";

        Stack<string> userStack = new Stack<string>(_sm.str_Answers.ToArray());

        while (userStack.Count > 0)
        {
            string back = userStack.Pop();

            _result += "\n";
            _result += back;
            _result += "\n--------------------------------";
        }
        // ai를 통해 정보 가져오기
        yield return StartCoroutine(NursingChatClient.SendQuestionToAPIUsing(_result));

        // 답변 리스트 반환
        List<string> qSentences = GetQList(aiResponse.answer);

        // 답변 카드 생성
        DisplayAnswerCards(qSentences, userStack);

        Obj_Wait.SetActive(false);

        // dotween
        RectTransform rectContent = Obj_Content.GetComponent<RectTransform>();

        rectContent.DOAnchorPos(new Vector2(0f, 0f), DG_Time).SetEase(DG_Ease);

        RectTransform rect_title = Obj_Title.transform.GetComponent<RectTransform>();

        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);
        //------


        yield return new WaitForSeconds(DG_Time);


    }

    void DisplayAnswerCards(List<string> aiAnswers, Stack<string> userAnswersStack)
    {
        List<string> userAnswers = new List<string>(userAnswersStack);

        for (int i = 0; i < userAnswers.Count; i++)
        {
            string aiAnswer = aiAnswers[i];
            string userAnswer = userAnswers[i];


        }
    }

    List<string> GetQList(string rawText)
    {
        // 정규식으로 "Q숫자."로 시작해서 다음 Q숫자. 또는 📊, 🎯, 끝까지 추출
        string pattern = @"Q\d+\..*?(?=Q\d+\.|📊|🎯|$)";
        MatchCollection matches = Regex.Matches(rawText, pattern, RegexOptions.Singleline);

        List<string> qSentences = new List<string>();

        foreach (Match match in matches)
        {
            qSentences.Add(match.Value.Trim());
        }

        return qSentences;
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
