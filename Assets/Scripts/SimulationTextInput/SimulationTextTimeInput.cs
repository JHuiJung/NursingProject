using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class SimulationTextTimeInput : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10), SerializeField]
    GameObject Obj_CanvasChoice;
    [SerializeField]
    GameObject Obj_AreaTextInput;
    [SerializeField]
    GameObject Obj_BTN_Submit;


    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    // -------------시간 입력 모드-------------
    [Header("정답 시간 텀(필수로 입력)")]
    public int timeInput_Offset;

    [Header("텍스트 입력")]
    public TMP_InputField textInputField;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnBegin;
    public UnityEngine.Events.UnityEvent OnEnd;

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -20f;
    public float DG_Area_StartY = -450f;
    public Ease DG_Ease = Ease.Linear;

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

        Setup();

        StartCoroutine(AllUiOn());

        OnBegin?.Invoke(); // 시작 이벤트 호출
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

        //버튼 활성화 or 비활성화
        if (string.IsNullOrWhiteSpace(textInputField.text))
        {
            Obj_BTN_Submit.SetActive(false);
        }
        else
        {
            Obj_BTN_Submit.SetActive(true);
        }
    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
        OnEnd?.Invoke(); // 종료 이벤트 호출
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;
        Obj_BTN_Submit.SetActive(false);
        textInputField.text = string.Empty;
    }

    //------------------------------------------------------------------------------------------

    void Setup()
    {
        //시나리오 메니저에서 현재 게임 시간 가져오기
        string slectedAnswer = _sm.inGameTime;

        // 정답 설정 ( 정답 시간 + Offset )
        timeInput_Answer = GetTimeWithMinutesAdded(slectedAnswer, 20);

        // 질문 텍스트 수정
        Tmp_Question.text = text_Question + $"\n[ 현재 시간 : {slectedAnswer} ]";

        //print(timeInput_Answer);
    }

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        isSimulationEnd = true;
        Obj_BTN_Submit.SetActive(false);

        string answer = textInputField.text;

        // 정답 스택에 추가
        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = $"Answer :  {timeInput_Offset} 만큼 지난 시간인 {timeInput_Answer} 이 정답";
        submitForm.txt_userAnswer = answer;
        _sm.str_Answers.Push(submitForm);

        StartCoroutine(AllUiOff());
    }

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // 텍스트 입력 DG
        RectTransform rect_AreaTI = Obj_AreaTextInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_EndY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);
    }

    IEnumerator AllUiOff()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        // 텍스트 입력 DG
        RectTransform rect_AreaTI = Obj_AreaTextInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_StartY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);

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
