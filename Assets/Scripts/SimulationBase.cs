using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SimulationBase : MonoBehaviour
{
    [Header("Simulation index")]
    public int simulation_Quiz_Index = 0; // ½Ã¹Ä·¹ÀÌ¼Ç ID
    
    public abstract void Enter(ScenarioManager SM);
    public abstract void Excute(ScenarioManager SM);
    public abstract void Exit(ScenarioManager SM);

    public abstract void ResetSimulation();
}
