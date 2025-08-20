using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class Simulation_ChoiceRandImg : SimulationBase
{

    [Header("Canvas Obj & Stuff"), Space(10)]
    public GameObject Obj_CanvasChoice;
    public Simulation_RandImg simulation_RandImg;
    public bool useButtonSetup = true;
    public int answerNumber = 0;

    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    // 선택 버튼들
    [Header("선택 버튼들"), Space(10)]
    public List<BTN_Choice> BTN_Choices = new List<BTN_Choice>();

    //닷트윈 옵션
    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_BTN_EndX = 150f;
    public float DG_BTN_StartX = 900f;
    public Ease DG_Ease = Ease.InOutQuad;


    // 시뮬레이션 끝 bool
    public bool isSimulationEnd = false;
    SpriteAndTextAnswer selectedAnswer;
    ScenarioManager _sm;
    string text_Answer = "";

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 시작");

        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup();

        if (useButtonSetup) BTN_Setup();
        else BTN_NoSetup();
            StartCoroutine(AllUiOn());
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;
    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        foreach (BTN_Choice button in BTN_Choices)
        {
            button.BtnOff();
        }

        isSimulationEnd = false;
    }

    //------------------------------------------------------------------------------------------

    void BTN_Setup()
    {
        // 정답이 할당될 번호 가져오기
        int randBTN_Num = Random.Range(0, BTN_Choices.Count);

        // 버튼의 개수 -1  개 만큼의 더미 대답 뭉치 가져오기
        List<SpriteAndTextAnswer> dummies = GetRandomStrings(simulation_RandImg.spriteAndTextAnswer_Dummies, BTN_Choices.Count);
        int j = 0;

        // 정답 정보 가져오기
        selectedAnswer = simulation_RandImg.selectedTA;

        print($"{name} 의 정답 : {selectedAnswer.ls_Answers[answerNumber]}");

        // 버튼 초기화
        for (int i = 0; i < BTN_Choices.Count; i++)
        {
            BTN_Choices[i].SetBTN_RandImg(dummies[i].ls_Answers[answerNumber], i + 1, this);
        }
    }

    void BTN_NoSetup()
    {
        // 정답 정보 가져오기
        selectedAnswer = simulation_RandImg.selectedTA;

        // 버튼 초기화
        for (int i = 0; i < BTN_Choices.Count; i++)
        {
            BTN_Choices[i].SetBTN_RandImg("", i + 1, this);
        }
    }

    void Setup()
    {
        // 질문 텍스트 수정
        Tmp_Question.text = text_Question;
    }

    public void SubmitAnswer(string answer, int choosedNum)
    {
        if (isSimulationEnd) return;
        isSimulationEnd = true;

        print($"제출된 문항 : {answer} / 선택 번호 : {choosedNum}");

        if (answer == selectedAnswer.ls_Answers[answerNumber])
        {
            // 정답 
            print($"{answer} : 은 정답 맞죠!");

        }
        else
        {
            // 오답
            print($"{answer} 은 오답 {selectedAnswer.ls_Answers[answerNumber]} 이 정답");

        }

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = Tmp_Question.text;
        submitForm.txt_QuestionAnswer = selectedAnswer.ls_Answers[answerNumber];
        submitForm.txt_userAnswer = answer;
        submitForm.quiz_index = simulation_Quiz_Index;
        _sm.str_Answers.Add(submitForm);


        StartCoroutine(AllUiOff());
    }

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);


        // 버튼 DG
        for (int i = 0; i < BTN_Choices.Count; ++i)
        {
            RectTransform rect = BTN_Choices[i].GetComponent<RectTransform>();

            rect.DOAnchorPos(new Vector2(DG_BTN_EndX, rect.anchoredPosition.y), DG_Time - i * 0.1f)
                .SetEase(DG_Ease);
        }

        yield return new WaitForSeconds(DG_Time);
    }

    IEnumerator AllUiOff()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);


        // 버튼 DG
        for (int i = 0; i < BTN_Choices.Count; ++i)
        {
            RectTransform rect = BTN_Choices[i].GetComponent<RectTransform>();

            rect.DOAnchorPos(new Vector2(DG_BTN_StartX, rect.anchoredPosition.y), DG_Time - i * 0.1f)
                .SetEase(DG_Ease);
        }

        yield return new WaitForSeconds(DG_Time);

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();
    }

    // 중복 없이 랜덤으로 count개 선택하는 함수
    List<SpriteAndTextAnswer> GetRandomStrings(List<SpriteAndTextAnswer> sourceList, int count)
    {
        if (sourceList.Count < count)
        {
            Debug.LogWarning("Source list has fewer elements than requested count.");
            return new List<SpriteAndTextAnswer>(sourceList); // 가능한 만큼만 반환
        }

        List<SpriteAndTextAnswer> shuffled = sourceList.OrderBy(x => Random.value).ToList();
        return shuffled.Take(count).ToList();
    }
}
