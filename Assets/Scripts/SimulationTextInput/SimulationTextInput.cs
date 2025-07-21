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
    public List<string> list_TimeAnswer = new List<string>();


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



        //리스트의 시간중 하나 선택
        string slectedAnswer = list_TimeAnswer[UnityEngine.Random.Range(0, list_TimeAnswer.Count)];

        // 질문 텍스트 수정
        Tmp_TimeInput_Question.text = text_TimeInput_Question + $"\n[ 현재 시간 : {GetTimeWithMinutesAdded(slectedAnswer, -20)} ]";
        
        // 정답 설정 ( 정답 시간 + Offset )
        timeInput_Answer = slectedAnswer;

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

    string GetTimeWithMinutesAdded(string timeString, int minutesToAdd)
    {
        // 입력 문자열 파싱: "6am", "6am 15m" 등
        string[] parts = timeString.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string timePart = parts[0]; // "6am" 또는 "7pm"
        int minutePart = 0;

        // 분 정보가 있는 경우 파싱
        if (parts.Length > 1 && parts[1].EndsWith("m"))
        {
            minutePart = int.Parse(parts[1].TrimEnd('m'));
        }

        // 시간과 AM/PM 분리
        int hour = int.Parse(new string(timePart.Where(char.IsDigit).ToArray()));
        string period = timePart.EndsWith("pm") ? "pm" : "am";

        // 12시간제 -> 24시간제 변환
        if (period == "pm" && hour != 12)
            hour += 12;
        if (period == "am" && hour == 12)
            hour = 0;

        // 기준 DateTime 생성
        DateTime baseTime = new DateTime(1, 1, 1, hour, minutePart, 0);

        // 분 추가
        DateTime resultTime = baseTime.AddMinutes(minutesToAdd);

        // 24시간제 -> 12시간제 변환
        string resultPeriod = resultTime.Hour >= 12 ? "pm" : "am";
        int resultHour = resultTime.Hour % 12;
        if (resultHour == 0) resultHour = 12;
        int resultMinute = resultTime.Minute;

        // 결과 문자열 생성
        if (resultMinute == 0)
        {
            return $"{resultHour}{resultPeriod}";
        }
        else
        {
            return $"{resultHour}{resultPeriod} {resultMinute}m";
        }
    }

}
