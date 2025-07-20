using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static UnityEngine.Rendering.DebugUI;

public class SimulationTextInput : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10), SerializeField]
    GameObject Obj_CanvasChoice;


    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    // 정답
    [Header("정답(필수로 입력)"), Space(10)]
    public string text_Answer = "";

    // -------------시간 입력 모드-------------
    [Header("---------- 시간 입력 모드 ----------"), Space(10)]
    public bool isTimeInputMode = false;
    [TextArea]
    public string text_TimeInput_Question = "";
    public TMP_Text Tmp_TimeInput_Question;


    [Header("정답 시간 텀(필수로 입력)"), Space(10)]
    public int timeInput_Offset;

    // -------------텍스트 입력-------------
    [Header("---------- 텍스트 입력 ----------"), Space(10)]
    public TMP_InputField textInputField;
    public UnityEngine.UI.Button BTN_Submit;

    //
    string timeInput_Answer = "";

    // 시뮬레이션 끝 bool
    bool isSimulationEnd = false;

    ScenarioManager _sm;

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 시작");

        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        if(isTimeInputMode)
        {
            Setup_TimeInput();
        }
        else
        {
            Setup_Normal();
        }
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

        //버튼 활성화 or 비활성화
        if(string.IsNullOrWhiteSpace(textInputField.text))
        {
            BTN_Submit.interactable = false;
        }
        else {
            BTN_Submit.interactable = true;
        }
    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;
    }

    //------------------------------------------------------------------------------------------

    void Setup_Normal()
    {
        // 질문 텍스트 수정
        Tmp_Question.text = text_Question;

        
    }

    void Setup_TimeInput()
    {

        // 현재 시간 가져오기
        DateTime now = DateTime.Now;

        // 질문 텍스트 수정
        Tmp_TimeInput_Question.text = text_TimeInput_Question + $"\n[ 현재 시간 : {GetTimeWithMinutesAdded(now, 0)} ]";
        
        // 정답 설정
        timeInput_Answer = GetTimeWithMinutesAdded(now,timeInput_Offset);

        print(timeInput_Answer);

    }

    public void SubmitAnswer()
    {
        isSimulationEnd = true;
        BTN_Submit.interactable = false;
        string answer = textInputField.text;

        if (isTimeInputMode)
        {

            if (answer == timeInput_Answer)
            {
                print($"텍스트 입력 : TimeInputMode {timeInput_Answer} 은 정답!");
            }
            else
            {
                print($"텍스트 입력 : TimeInputMode {timeInput_Answer} 은 정답아님");
            }

        }
        else
        {
            if (answer == text_Answer)
            {
                print($"텍스트 입력 : NormalMode {text_Answer} 은 정답!");
            }
            else
            {
                print($"텍스트 입력 : NormalMode {text_Answer} 은 정답아님");
            }
        }

        

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();

    }

    string GetTimeWithMinutesAdded(DateTime now,int m)
    {
        // m분 추가
        DateTime futureTime = now.AddMinutes(m);

        // 시간과 분 추출
        int hour = futureTime.Hour;
        int minute = futureTime.Minute;

        // AM/PM 처리
        string period = hour >= 12 ? "pm" : "am";
        int displayHour = hour % 12;
        if (displayHour == 0) displayHour = 12; // 0시는 12로 표시

        // 분이 0이면 "6am", 아니면 "6am 15m"
        if (minute == 0)
        {
            return $"{displayHour}{period}";
        }
        else
        {
            return $"{displayHour}{period} {minute}m";
        }
    }

}
