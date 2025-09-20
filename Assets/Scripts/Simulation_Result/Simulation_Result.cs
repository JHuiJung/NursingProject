using DarkTonic.MasterAudio;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static DataManager;

public class Simulation_Result : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10)]
    public GameObject Obj_CanvasChoice;
    public GameObject Obj_BTN_Next;
    public GameObject Pf_Result_Session_Card;
    public GameObject Obj_Area_Result_SesstionCard;

    [Header("Txts"), Space(10)]
    public TMP_Text txt_Total_Questions;
    public TMP_Text txt_Total_O;
    public TMP_Text txt_Total_X;
    public TMP_Text txt_Total_Rate;

    public bool isSimulationEnd = false;
    private ScenarioManager _sm;

    public override void Enter(ScenarioManager SM)
    {
        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup();
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

        // 세션 카드 제거
        int childCnt = Obj_Area_Result_SesstionCard.transform.childCount;
        for(int i = childCnt -1; i >= 0; --i)
        {
            Transform child = Obj_Area_Result_SesstionCard.transform.GetChild(i);
            Destroy(child.gameObject);
        }
    }


    //------------------------------------------------------------------------------------------

    void Setup()
    {
        // 버튼 활성화
        Obj_BTN_Next.SetActive(true);

        // Gage off
        _sm.GageObjSetActive(false);

        // 전체 점수 업데이트
        txt_Total_Questions.text = $"전체 문제 수 : {ScenarioManager.inst.score_Totalcnt}개";
        txt_Total_O.text = $"전체 정답 수 : {ScenarioManager.inst.score_totalCorrect}개";
        txt_Total_X.text = $"전체 오답 수 : {ScenarioManager.inst.score_totalinCorrect}개";
        float percentage = ScenarioManager.inst.score_Totalcnt > 0 ?
    (float)ScenarioManager.inst.score_totalCorrect / ScenarioManager.inst.score_Totalcnt * 100 : 0f;
        txt_Total_Rate.text = $"{percentage:F1}%";

        // 세션 카드 생성
        JsonScoreData jsonScoreData = DataManager.inst.jsonScoreData;
        List<ScoreSaveForm> ls_ScoreSaveForm = jsonScoreData.ls_scoreSaveForm;

        foreach (var scoreSaveForm in ls_ScoreSaveForm)
        {
            GameObject card = Instantiate(Pf_Result_Session_Card, Obj_Area_Result_SesstionCard.transform);
            Result_SessionCard sessionCard = card.GetComponent<Result_SessionCard>();
            sessionCard.SetCard(scoreSaveForm.sesstionName, scoreSaveForm.score_Correct_Cnt, 
                scoreSaveForm.score_InCorrect_Cnt, scoreSaveForm.score_Percentage);
        }

    }

    public void Next()
    {
        isSimulationEnd = true;

        MasterAudio.PlaySound("Button_Press");
        Obj_BTN_Next.SetActive(false);
        
        _sm.NextSimulation();
    }
}
