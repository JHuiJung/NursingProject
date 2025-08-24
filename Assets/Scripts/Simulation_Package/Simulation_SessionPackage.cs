using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Simulation_SessionPackage : SimulationBase
{
    // 실패시 돌아올 인덱스( 1번 부터 시작 )
    public int returnIndex = 1;

    //플레이어 이동 경로
    public List<Vector3> playerMovePositions = new List<Vector3>();

    [Header("Time Check")]
    public int timeThreshold = 180; // 몇초 후에 타임오버
    public TMP_Text txt_TimeLeft;
    public GameObject Obj_Area_TimeOut;

    [Header("Reset Objects")]
    public List<Objs_SetupForm> objs_SetupForms = new List<Objs_SetupForm>();

    float _time = 0f;
    ScenarioManager _sm;
    private Coroutine timeCheckRoutine;

    public override void Enter(ScenarioManager SM)
    {
        _sm = SM;

        _time = 0f;

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
        _time = 0f;
    }

    void LoadSimulations()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            SimulationBase simulationBase = transform.GetChild(i).GetComponent<SimulationBase>();

            _sm.simulationBases.Add(simulationBase);
        }
    }

    public void ResetObjs()
    {
        CameraManager.inst.SetCamera("Player");

        foreach(Objs_SetupForm obj in objs_SetupForms)
        {
            GameObject targetObj = CameraManager.inst.GetGameObject(obj.obj_name);

            if (targetObj != null)
            {
                targetObj.transform.position = obj.obj_position;
                targetObj.transform.eulerAngles = obj.obj_rotation;
                targetObj.SetActive(obj.is_active);
            }
            else
            {
                Debug.Log($"[Simulation_SessionPackage] : {obj.obj_name} not found in the scene.");
            }
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

    public void NextSimulation()
    {
               _sm.NextSimulation();
    }

    public void Start_TimeCheck()
    {
        timeCheckRoutine = StartCoroutine(Co_Start_TimeCheck());
    }

    public IEnumerator Co_Start_TimeCheck()
    {
        _time = 0f;

        while (_time < timeThreshold)
        {
            // 1초 단위로 갱신
            yield return new WaitForSeconds(1f);

            _time += 1f;
            int timeLeft = timeThreshold - Mathf.FloorToInt(_time);

            txt_TimeLeft.text = $"남은 시간 : {timeLeft}초";
        }

        // 시간이 다 됐을 때 처리
        txt_TimeLeft.text = "시간 종료!";

        Obj_Area_TimeOut.SetActive(true);
        
    }


    public void Stop_TimeCheck()
    {
        if (timeCheckRoutine != null)
        {
            StopCoroutine(timeCheckRoutine);
            timeCheckRoutine = null;
        }

        _time = 0f;
        txt_TimeLeft.text = $"";
    }

    public void ReturnSimultion()
    {
        _sm.ReturnSimultion();
    }
}

[System.Serializable]
public class Objs_SetupForm
{
    public string obj_name = "";
    public Vector3 obj_position = Vector3.zero;
    public Vector3 obj_rotation = Vector3.zero;
    public bool is_active = true;
}
