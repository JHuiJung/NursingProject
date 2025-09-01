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

    Simulation_FeedBack simulation_FeedBack;
    Simulation_FeedBack_Imp simulation_FeedBack_Imp;

    public void Setup(float passThreshold, ChatResponse userResponse)
    {
        simulation_FeedBack = transform.parent.parent.parent.GetComponent<Simulation_FeedBack>();
        simulation_FeedBack_Imp = transform.parent.parent.parent.GetComponent<Simulation_FeedBack_Imp>();

        int O = userResponse.correct_count;
        int X = userResponse.incorrect_count;
        float rate = userResponse.score_percentage;

        txt_Title.text = $"결과 요약\r\n( 합격 기준 : 정답률 {passThreshold}%  )";
        txt_CorrectRate.text = $"정답률 : {rate:F1}%";
        txt_O.text = $" {O} 개";
        txt_X.text = $" {X} 개";

        bool isPass = rate >= passThreshold;

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
        if(simulation_FeedBack != null)
        {
            simulation_FeedBack.Pass();
        }
        else if(simulation_FeedBack_Imp != null)
        {
            simulation_FeedBack_Imp.Pass();
        }
            
    }

    public void NonPass()
    {
        if (simulation_FeedBack != null)
        {
            simulation_FeedBack.NonPass();
        }
        else if (simulation_FeedBack_Imp != null)
        {
            simulation_FeedBack_Imp.NonPass();
        }
        
    }

}
