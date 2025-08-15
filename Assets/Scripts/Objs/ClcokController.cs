using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class ClcokController : MonoBehaviour
{
    public Transform pointer_hour;  // 시침
    public Transform pointer_min;   // 분침
    public List<string> ls_randTimeStrings; // 시간 문자열 목록 예: ["6am", "5am 40m"]

    private void Start()
    {
        SetRandomTime();
    }

    [ContextMenu("Set Time from String Random")]
    public void SetRandomTime()
    {
        // 리스트의 랜덤한 시간 결정
        string timeString = ls_randTimeStrings[Random.Range(0, ls_randTimeStrings.Count)];

        // 시나리오 매니저 인게임 시간 업데이트
        ScenarioManager.inst.inGameTime = timeString;

        // 정규식: 시, am/pm, 선택적 분 추출
        Match match = Regex.Match(timeString.ToLower(), @"(\d{1,2})(am|pm)\s*(\d{1,2})?m?");
        if (!match.Success)
        {
            Debug.LogError("시간 문자열 형식이 올바르지 않습니다: " + timeString);
            return;
        }

        int hour = int.Parse(match.Groups[1].Value);
        string ampm = match.Groups[2].Value;
        int minute = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        // AM/PM 변환 (12시간제 → 0~11 범위)
        if (ampm == "am")
        {
            if (hour == 12) hour = 0; // 12am은 0시
        }
        else // pm
        {
            if (hour != 12) hour += 12; // 12pm은 그대로 12시
        }

        // 시침 각도: 한 시간당 30도 + 분에 따른 추가 각도
        float hourAngle = ((hour % 12 + minute / 60f) * 30f);
        // 분침 각도: 한 분당 6도
        float minuteAngle = (minute * 6f);

        // z축 회전 적용
        pointer_hour.localEulerAngles = new Vector3(0, 0, hourAngle);
        pointer_min.localEulerAngles = new Vector3(0, 0, minuteAngle);
    }
}
