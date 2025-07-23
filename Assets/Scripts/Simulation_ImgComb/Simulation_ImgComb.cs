using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Simulation_ImgComb : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10)]
    public GameObject Obj_CanvasChoice;


    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    public List<ImgComb_AnswerSpace> answerSpaces = new List<ImgComb_AnswerSpace>();
    public List<ImgComb_Entity> imgComb_Entities = new List<ImgComb_Entity>();

    [SerializeField] GameObject Obj_Button;

    // 시뮬레이션 끝 bool
    [NonReorderable]
    private bool isSimulationEnd = false;
    [NonReorderable]
    private ScenarioManager _sm;

    string userAnswer = "";

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 시작");

        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup();
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

        foreach(ImgComb_AnswerSpace imgComb_Answer in answerSpaces)
        {
            imgComb_Answer.CheckFind();
        }

        // 전부다 채워짐
        if(CheckIsAllFilled())
        {
            Obj_Button.SetActive(true);
        }
        else
        {
            Obj_Button.SetActive(false);
        }

        
    }

    public void Submit()
    {
        isSimulationEnd = true;

        imgComb_Entities.Sort((a, b) =>
        {
            int numA = int.TryParse(a.number, out var nA) ? nA : int.MaxValue;
            int numB = int.TryParse(b.number, out var nB) ? nB : int.MaxValue;
            return numA.CompareTo(numB);
        });


        userAnswer += text_Question + "/ User Answer : ";


        for (int i = 0; i < imgComb_Entities.Count; i++) {

            ImgComb_Entity e = imgComb_Entities[i];

            userAnswer += $"[ {e.number}번 : {e.entity_Title}]";

            if(i != imgComb_Entities.Count - 1)
            {
                userAnswer += " -> ";
            }
            else
            {
                userAnswer += "\n";
            }


        }

        print($"{name} : {userAnswer}");

        _sm.str_Answers.Push(userAnswer);

        _sm.NextSimulation();

    }


    bool CheckIsAllFilled()
    {
        foreach (ImgComb_AnswerSpace imgComb_Answer in answerSpaces)
        {
            if (!imgComb_Answer.isFilled)
                return false;
        }

        return true;
    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
        userAnswer = "";
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

        
    }

    public virtual void SubmitAnswer(string answer, int choosedNum)
    {
        isSimulationEnd = true;

        //정답 스택에 추가
        _sm.str_Answers.Push($"{text_Question} / User Answer : {answer}");

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();

    }
}
