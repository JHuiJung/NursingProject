using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static NursingChatClient;

public class FeedBackResultCard : MonoBehaviour
{
    [Header("Txt"), Space(10)]
    public TMP_Text txt_Title;
    public TMP_Text txt_O;
    public TMP_Text txt_X;
    public TMP_Text txt_CorrectRate;
    public TMP_Text txt_Result;

    [Header("BTNs"), Space(10)]
    public GameObject obj_Btn_Pass;
    public GameObject obj_Btn_NonPass;

    int _pass_MoveSimulationIndex = 0;

    public void Setup(float passThreshold, ChatResponse userResponse, int pass_MoveSimulationIndex)
    {
        int O = userResponse.correct_count;
        int X = userResponse.incorrect_count;
        float rate = userResponse.score_percentage;

        txt_Title.text = $"결과 요약\r\n( 합격 기준 : 정답률 {passThreshold}%  )";
        txt_CorrectRate.text = $"정답률 : {rate}%";
        txt_O.text = $" {O} 개";
        txt_X.text = $" {X} 개";

        bool isPass = rate >= passThreshold;

        _pass_MoveSimulationIndex = pass_MoveSimulationIndex;

        if (isPass)
        {
            txt_Result.text = "<color=#BEFFA3>합격</color>";

            obj_Btn_Pass.SetActive(true);
        }
        else
        {
            txt_Result.text = "<color=#FF7A7A>불합격</color>";

            obj_Btn_NonPass.SetActive(true);
        }

    }

    public void Pass()
    {
        ScenarioManager.inst.NextSimulation();
    }

    public void NonPass()
    {
        ScenarioManager.inst.MoveSimulation(_pass_MoveSimulationIndex);
    }

}
