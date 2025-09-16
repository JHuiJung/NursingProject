using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;
using DarkTonic.MasterAudio;
using static DataManager;

public class Admin_Func : MonoBehaviour
{
    public GameObject Area_Wait;

    public List<DataManager.GoogleData> Ls_GoogleData = new List<DataManager.GoogleData>();

    [Header("Objs")]
    public GameObject pf_GD_UserInfo;
    public GameObject Area_GD_UserInfo;

    [Header("Scenario Info")]
    public GameObject Area_ScenarioInfo;
    public GameObject Area_SessionCards;
    public GameObject Pf_Result_Session_Card;
    public TMP_Text txt_Scenario;
    public TMP_Text txt_Name;
    public TMP_Text txt_Id;
    public TMP_Text txt_Date;
    public TMP_Text txt_playTime;
    public TMP_Text txt_Total_Qcnt;
    public TMP_Text txt_Total_CorrectCnt;
    public TMP_Text txt_Total_inCorrectCnt;
    public TMP_Text txt_Total_Rate;

    [Header("Txts")]
    public TMP_InputField InputField_id;
    public TMP_Text txt_Info;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetUserInfo()
    {
        MasterAudio.PlaySound("Button_Press");

        // 이전 기록 지우기
        txt_Info.text = "...";

        // 유저 정보 카드 지우기
        for (int i = Area_GD_UserInfo.transform.childCount - 1; i >= 0; i--)
        {
            GameObject obj = Area_GD_UserInfo.transform.GetChild(i).gameObject;

            Destroy(obj);
        }

        StartCoroutine(CoGetUserInfo());
    }

    IEnumerator CoGetUserInfo()
    {
        Area_Wait.SetActive(true);

        string _id = InputField_id.text;

        yield return StartCoroutine(DataManager.inst.CoGetUserInfo(_id));

        Ls_GoogleData = DataManager.inst.GetGoogleDatas();

        Area_Wait.SetActive(false);

        if (Ls_GoogleData.Count == 0 )
        {
            txt_Info.text = $"{_id} 에 해당하는 데이터가 없습니다";
        }
        else
        {
            // 데이터 있음

            txt_Info.text = $"학생 이름 : {Ls_GoogleData[0]._name} / 학번 : {Ls_GoogleData[0]._id}";


            // 유저 정보 카드 생성
            for(int i=0; i<Ls_GoogleData.Count; i++)
            {
                var np = Instantiate(pf_GD_UserInfo, Area_GD_UserInfo.transform);
                np.GetComponent<Admin_UserInfo_Card>().Setup(Ls_GoogleData[i],this);
            }

            


        }

        
    }

    public void OpenInfo(DataManager.GoogleData _googleData)
    {
        txt_Scenario.text = $"시나리오 : {_googleData._scenario}";
        txt_Name.text = $"이름 : {_googleData._name}";
        txt_Id.text = $"학번 : {_googleData._id}";
        txt_Date.text = $"수행 날짜 : {_googleData._date}";

        string playTime = _googleData._totalTime.Split(' ')[4];

        txt_playTime.text = $"총 시간 : {playTime}";

        JsonScoreData scoreData = JsonUtility.FromJson<JsonScoreData>(_googleData._score);

        txt_Total_Qcnt.text = $"{scoreData.score_total_Cnt} 개";
        txt_Total_CorrectCnt.text = $"{scoreData.score_total_Correct_Cnt} 개";
        txt_Total_inCorrectCnt.text = $"{scoreData.score_total_inCorrect_Cnt} 개";
        txt_Total_Rate.text = $"{scoreData.score_Percentage:F1} %";

        // 카드 생성
        for(int i = 0; i < scoreData.ls_scoreSaveForm.Count; ++i)
        {
            string title = scoreData.ls_scoreSaveForm[i].sesstionName;
            int O = scoreData.ls_scoreSaveForm[i].score_Correct_Cnt;
            int X = scoreData.ls_scoreSaveForm[i].score_InCorrect_Cnt;
            float rate = scoreData.ls_scoreSaveForm[i].score_Percentage;
            var np = Instantiate(Pf_Result_Session_Card, Area_SessionCards.transform);
            np.GetComponent<Result_SessionCard>().SetCard(title, O, X, rate);
        }

        Area_ScenarioInfo.SetActive(true);
    }

    public void CloseInfo()
    {
        MasterAudio.PlaySound("Button_Press");
        Area_ScenarioInfo.SetActive(false);

        for (int i = Area_SessionCards.transform.childCount - 1; i >= 0; i--)
        {
            GameObject obj = Area_SessionCards.transform.GetChild(i).gameObject;

            Destroy(obj);
        }
    }

}
