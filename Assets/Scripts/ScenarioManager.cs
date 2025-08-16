using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SubmitForm
{
    public string txt_Question;
    public string txt_userAnswer;
    public string txt_QuestionAnswer;
}
public class ScenarioManager : MonoBehaviour
{
    public static ScenarioManager inst;
    // 시뮬레이션

    [Header("Simulation & Scenario")]
    public List<SimulationBase> simulationBases = new List<SimulationBase>();
   
    public Stack<SubmitForm> str_Answers = new Stack<SubmitForm>();

    SimulationBase currentSimulation = null;

    [SerializeField]
    int currentNumber = 0;

    [SerializeField]
    bool isScenarioEnd = false;

    [Header("Game info ")]
    public string inGameTime = "6am";

    //---- 점수 계산 ----
    public int score_totalCorrect = 0;
    public int score_totalinCorrect = 0;
    public int score_Totalcnt = 0;

    //--- 시간 측정
    public string totalTime = "None";
    private DateTime startTime;
    private DateTime endTime;

    //---- 

    private void Awake()
    {
        inst = this;
    }

    private void Start()
    {
        // 기초 초기화
        Setup();
        StartScenario();
    }

    private void Update()
    {
        if (currentSimulation != null)
        {
            currentSimulation.Excute(this);
        }
    }

    [ContextMenu("Next Simulation")]
    public void NextSimulation()
    {
        //시나리오 종료시 실행 안함
        if (isScenarioEnd) return;

        // 현재 번호 증가
        currentNumber++;

        // 현재 시나리오 번호가 끝일 경우 End 함수 실행
        if (currentNumber >= simulationBases.Count)
        {
            EndScenario();
            return;
        }

        // 현재 시뮬레이션이 있으면 탈출
        if(currentSimulation != null)
        {
            currentSimulation.Exit(this);
        }

        currentSimulation = simulationBases[currentNumber];
        currentSimulation.Enter(this);
    }

    public void EndScenario()
    {
        print($"해당 시나리오 종료 됨");
        isScenarioEnd = true;
        score_totalCorrect = 0;
        score_totalinCorrect = 0;
        score_Totalcnt = 0;
}

    [ContextMenu("Start Scenario")]
    public void StartScenario()
    {
        //currentSimulation 할당
        currentSimulation = simulationBases[currentNumber];
        currentSimulation.Enter(this);

        // isScenarioEnd false 로 수정
        isScenarioEnd = false;

        //--- 전체 시간 측정 시작 ----
        TimeCntStart();
    }

    public void MoveSimulation(int index)
    {
        //시나리오 종료시 실행 안함
        if (isScenarioEnd) return;

        // 현재 번호 index로 설정
        currentNumber = index;

        // 현재 시뮬레이션이 있으면 탈출
        if (currentSimulation != null)
        {
            currentSimulation.Exit(this);
        }

        currentSimulation = simulationBases[currentNumber];
        currentSimulation.Enter(this);
    }

    void Setup()
    {
        //simulationControllers 채우기

        int childCount = this.transform.childCount;

        // 자식 오브젝트 중 SimulationController가 있을 경우 리스트에 추가
        for (int i = 0; i < childCount; i++)
        {
            SimulationBase childSC = 
                this.transform.GetChild(i).GetComponent<SimulationBase>();
        
            if(childSC != null)
            {
                simulationBases.Add(childSC);
            }
        }

        
    }

    // 타이머 시작
    public void TimeCntStart()
    {
        startTime = DateTime.Now;
        Debug.Log("타이머 시작: " + startTime.ToString("yyyy.MM.dd HH:mm:ss"));
    }

    // 타이머 종료
    public void TimeCntEnd()
    {
        endTime = DateTime.Now;
        Debug.Log("타이머 종료: " + endTime.ToString("yyyy.MM.dd HH:mm:ss"));

        TimeSpan duration = endTime - startTime;
        Debug.Log("총 경과 시간: " + duration.ToString(@"hh\:mm\:ss"));

        totalTime = duration.ToString(@"hh\:mm\:ss");
    }
}
