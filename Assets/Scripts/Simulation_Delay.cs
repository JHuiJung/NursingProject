using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation_Delay : SimulationBase
{
    public bool isInfinity = false;

    public float DelayTime = 1f;

    float _time = 0f;

    public override void Enter(ScenarioManager SM)
    {
    }

    public override void Excute(ScenarioManager SM)
    {
        // 무한이면 계속 유지
        if (isInfinity) return;

        _time += Time.deltaTime;

        if(_time > DelayTime)
        {
            _time = 0f;
            SM.NextSimulation();
        }

    }

    public override void Exit(ScenarioManager SM)
    {
        _time = 0f;
    }

    public override void ResetSimulation()
    {

    }
}
