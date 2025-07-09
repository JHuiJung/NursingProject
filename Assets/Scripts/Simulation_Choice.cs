using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation_Choice : SimulationBase
{

    [TextArea] //질문
    public string text_Question = "";

    // 정답
    public string text_Answer = "";

    public List<string> text_Dummies = new List<string>();

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 시작");
    }

    public override void Excute(ScenarioManager SM)
    {
    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
    }

    //------------------------------------------------------------------------------------------

    void Setup()
    {
        // 문제 랜덤으로 설정

        // 정답 문제 할당
    }
}
