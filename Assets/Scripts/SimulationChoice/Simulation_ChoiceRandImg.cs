using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

[System.Serializable]
public class SpriteAndTextAnswer
{
    public Sprite sprite;
    public string text_Answer;
}

public class Simulation_ChoiceRandImg : Simulation_Choice
{
    // 더미 뭉치
    [Header("답 더미 뭉치"), Space(10)]
    public List<SpriteAndTextAnswer> spriteAndTextAnswer_Dummies = new List<SpriteAndTextAnswer>();

    // 선택 버튼들
    [Header("Obj 이미지"), Space(10)]
    public Image Obj_targetImg;

    // 시뮬레이션 끝 bool
    bool isSimulationEnd2 = false;
    SpriteAndTextAnswer selectedAnswer;
    ScenarioManager _sm2;

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 시작");

        Obj_CanvasChoice.SetActive(true);
        _sm2 = SM;

        Setup();
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd2) return;
    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        isSimulationEnd2 = false;
    }

    //------------------------------------------------------------------------------------------

    void Setup()
    {
        // 질문 텍스트 수정
        SetTitle(text_Question);

        // 정답이 할당될 번호 가져오기
        int randBTN_Num = Random.Range(0, BTN_Choices.Count);

        // 버튼의 개수 -1  개 만큼의 더미 대답 뭉치 가져오기
        List<SpriteAndTextAnswer> dummies = GetRandomStrings(spriteAndTextAnswer_Dummies, BTN_Choices.Count);
        int j = 0;

        // 무작위로 정답 할당
        selectedAnswer = dummies[randBTN_Num];

        print($"{name} 의 정답 : {selectedAnswer.text_Answer}");

        // 타겟 이미지에 선택된 이미지 삽입
        Obj_targetImg.sprite = selectedAnswer.sprite;

        // 버튼 텍스트 할당
        for (int i = 0; i < BTN_Choices.Count; i++)
        {
            BTN_Choices[i].SetBTN(dummies[i].text_Answer, i + 1, this);
        }
    }

    public void SetTitle(string str)
    {
        Tmp_Question.text = str;
    }

    public override void SubmitAnswer(string answer, int choosedNum)
    {
        isSimulationEnd2 = true;

        print($"제출된 문항 : {answer} / 선택 번호 : {choosedNum}");

        if (answer == selectedAnswer.text_Answer)
        {
            // 정답 
            print($"{answer} : 은 정답 맞죠!");

        }
        else
        {
            // 오답
            print($"{answer} 은 오답 {selectedAnswer.text_Answer} 이 정답");

        }


        // 이미지 타입(Opend, Crack 등), 선택한 문장, 선택한 번호, 정답 문장을 추가
        AddAnswerStack(selectedAnswer.text_Answer);

        // 다음 시뮬레이션으로 이동
        _sm2.NextSimulation();

    }

    public void AddAnswerStack(string ans)
    {
        _sm2.str_Answers.Push($"{Tmp_Question.text} / User Answer : {ans}");
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
