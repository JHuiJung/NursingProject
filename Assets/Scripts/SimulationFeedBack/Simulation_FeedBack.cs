using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation_FeedBack : SimulationBase
{

    bool isSimulationEnd = false;

    public override void Enter(ScenarioManager SM)
    {
    }

    public override void Excute(ScenarioManager SM)
    {
    }

    public override void Exit(ScenarioManager SM)
    {

        ResetSimulation();
    }

    public override void ResetSimulation()
    {
        isSimulationEnd = false;
    }
}
