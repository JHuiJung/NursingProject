using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;
using TMPro;

public class Simulation_Choice : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10)]
    public GameObject Obj_CanvasChoice;


    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    // 정답
    [Header("정답(필수로 입력 RandImg 제외)"), Space(10)]
    public string text_Answer = "";

    // 오답 뭉치
    [Header("오답 뭉치"),Space(10)]
    public List<string> text_Dummies = new List<string>();

    // 선택 버튼들
    [Header("선택 버튼들"), Space(10)]
    public List<BTN_Choice> BTN_Choices = new List<BTN_Choice>();

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.25f;
    public float DG_BTN_EndX = 150f;
    public float DG_BTN_StartX = 900f;
    public Ease DG_Ease = Ease.Linear;

    // 시뮬레이션 끝 bool
    [NonReorderable]
    public bool isSimulationEnd = false;
    [NonReorderable]
    private ScenarioManager _sm;

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
    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;
    }

    //------------------------------------------------------------------------------------------

    void Setup()
    {
        // 질문 텍스트 수정
        Tmp_Question.text = text_Question;

        // 정답이 할당될 번호 가져오기
        int randBTN_Num = Random.Range(0, BTN_Choices.Count);

        // 버튼의 개수 -1  개 만큼의 더미 대답 뭉치 가져오기
        List<string> dummy_strs = GetRandomStrings(text_Dummies, BTN_Choices.Count - 1);
        int j = 0;

        // 텍스트 할당
        for (int i = 0; i < BTN_Choices.Count; i++)
        {
            if(randBTN_Num == i)
            {
                BTN_Choices[i].SetBTN(text_Answer, i + 1, this);
            }
            else
            {
                BTN_Choices[i].SetBTN(dummy_strs[j], i + 1, this);
                j++;
            }
        }
    }

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x, 
            0f), DG_Time).SetEase(DG_Ease);


        // 버튼 DG
        for (int i = 0; i < BTN_Choices.Count;++i)
        {
            RectTransform rect = BTN_Choices[i].GetComponent<RectTransform>();

            rect.DOAnchorPos(new Vector2(DG_BTN_EndX, rect.anchoredPosition.y), DG_Time - i*0.1f)
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


    public virtual void SubmitAnswer(string answer, int choosedNum)
    {
        isSimulationEnd=true;

        print($"제출된 문항 : {answer} / 선택 번호 : {choosedNum}");

        if(answer == text_Answer)
        {
            // 정답 
            print($"{answer} : 은 정답 맞죠!"); 

        }
        else
        {
            // 오답
            print($"{answer} 은 오답 {text_Answer} 이 정답");

        }

        //정답 스택에 추가
        _sm.str_Answers.Push($"{text_Question} / User Answer : {answer}");


        // DG UI OFF
        StartCoroutine(AllUiOff());
    }

    // 중복 없이 랜덤으로 count개 선택하는 함수
    List<string> GetRandomStrings(List<string> sourceList, int count)
    {
        if (sourceList.Count < count)
        {
            Debug.LogWarning("Source list has fewer elements than requested count.");
            return new List<string>(sourceList); // 가능한 만큼만 반환
        }

        List<string> shuffled = sourceList.OrderBy(x => Random.value).ToList();
        return shuffled.Take(count).ToList();
    }

}
