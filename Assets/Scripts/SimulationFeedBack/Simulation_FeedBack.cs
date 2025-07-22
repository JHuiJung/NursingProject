using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class Simulation_FeedBack : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10),SerializeField]
    GameObject Obj_CanvasChoice;

    [SerializeField]
    TMP_Text tmp_Content;

    bool isSimulationEnd = false;
    ScenarioManager _sm;
    public override void Enter(ScenarioManager SM)
    {
        _sm = SM;

        // 화면 키기
        Obj_CanvasChoice.SetActive(true);

        // 화면에 유저가 선택한 텍스트 출력
        PrintUserAnswers();
    }

    public override void Excute(ScenarioManager SM)
    {
    }

    public override void Exit(ScenarioManager SM)
    {

        // 화면 끄기
        Obj_CanvasChoice.SetActive(false);
        ResetSimulation();

        //%%%%%%%%%%%%%%%%%%%% 임시로 피드백 종료시 스택 비우기 %%%%%%%%%%%%%%%%%%%%%%
        _sm.str_Answers.Clear();
    }

    public override void ResetSimulation()
    {
        isSimulationEnd = false;
    }

    void PrintUserAnswers()
    {
        string _result = "";

        Stack<string> userStack = new Stack<string>(_sm.str_Answers.ToArray());

        while(userStack.Count > 0)
        {
            string back = userStack.Pop();

            _result += "\n";
            _result += back;
            _result += "\n--------------------------------";
        }

        tmp_Content.text = _result;




    }
}
