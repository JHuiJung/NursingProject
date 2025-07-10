using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimulationBase : MonoBehaviour
{
    public abstract void Enter(ScenarioManager SM);
    public abstract void Excute(ScenarioManager SM);
    public abstract void Exit(ScenarioManager SM);

    public abstract void ResetSimulation();
}
