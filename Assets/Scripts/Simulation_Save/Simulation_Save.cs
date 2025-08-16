using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Simulation_Save : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10)]
    public GameObject Obj_CanvasChoice;
    public GameObject Obj_Area_Wait;

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -10f;
    public float DG_Area_StartY = -850f;
    public Ease DG_Ease = Ease.InOutQuad;

    public bool isSimulationEnd = false;
    private ScenarioManager _sm;

    public override void Enter(ScenarioManager SM)
    {
        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        StartCoroutine(StartSimulation());
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;
    }

    public override void Exit(ScenarioManager SM)
    {
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;
    }

    IEnumerator StartSimulation()
    {
        _sm.TimeCntEnd();

        Obj_Area_Wait.SetActive(true);

        yield return StartCoroutine( DataManager.inst.CoSave() );

        Obj_Area_Wait.SetActive(false);

        _sm.NextSimulation();

    }
}
