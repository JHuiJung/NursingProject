using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Simulation_Func : SimulationBase
{
    public UnityEvent events;
    public override void Enter(ScenarioManager SM)
    {
        events?.Invoke();
        SM.NextSimulation();
    }

    public override void Excute(ScenarioManager SM)
    {

    }

    public override void Exit(ScenarioManager SM)
    {

    }

    public override void ResetSimulation()
    {

    }
}
