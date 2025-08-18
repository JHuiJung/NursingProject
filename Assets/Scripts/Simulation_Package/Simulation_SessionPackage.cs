using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Simulation_SessionPackage : SimulationBase
{
    // 실패시 돌아올 인덱스( 1번 부터 시작 )
    public int returnIndex = 1;

    //플레이어 이동 경로
    public List<Vector3> playerMovePositions = new List<Vector3>();

    ScenarioManager _sm;

    public override void Enter(ScenarioManager SM)
    {
        _sm = SM;
        // 자식 오브젝트 한개씩 생성해 붙이기
        LoadSimulations();

        // 리턴 포인트 설정
        SM.returnPoint = _sm.currentNumber + returnIndex;

        // 다음 시물레이션으로 이동
        SM.NextSimulation();
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

    }

    void LoadSimulations()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            SimulationBase simulationBase = transform.GetChild(i).GetComponent<SimulationBase>();

            _sm.simulationBases.Add(simulationBase);

            //GameObject child = transform.GetChild(i).gameObject;

            //var Obj_Simulation = Instantiate(child,_sm.transform);
            //Obj_Simulation.name = child.name;
            //Obj_Simulation.transform.SetAsLastSibling();
        }
    }

    public void SetCamera(string cameraName)
    {
        // 카메라 매니저를 통해 카메라 설정
        CameraManager.inst.SetCamera(cameraName);
    }

    public void MovePLayer()
    {
        Player.inst.GotoPosition(playerMovePositions);
    }

    public void SetActivieObj_Off(string objName)
    {
        CameraManager.inst.SetActivieObj_Off(objName);
    }

    public void SetActivieObj_On(string objName)
    {
        CameraManager.inst.SetActivieObj_On(objName);
    }
}
