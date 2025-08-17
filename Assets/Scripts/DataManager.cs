using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static NursingChatClient;


public class DataManager : MonoBehaviour
{
    public static DataManager inst;

    public string userName = "홍길동";
    public string userID = "000000";

    public string patient_Information = "";

    [Header("Score Json Save Form")]
    public JsonScoreData jsonScoreData = new JsonScoreData();

    //---- google sheet ----
    const string URL = "https://script.google.com/macros/s/AKfycbw8jlHJTrJrFfFvg3IgFlkrgsvnj6zOt_WvazvhklhQwemtl1jbPhSUqY6W16FaeXM/exec";
    public GoogleData GD;

    private void Awake()
    {
        // 이미 인스턴스가 있고, 그것이 자기 자신이 아니라면 제거
        if (inst != null && inst != this)
        {
            Destroy(gameObject);
            return;
        }

        inst = this;
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지

        GD = new GoogleData();
    }

    // 셋업
    public void SetupUserInfo(string _name, string _id)
    {
        userName = _name;
        userID = _id;
    }

    [ContextMenu("Save")]
    public void Save()
    {
        StartCoroutine(CoSave());
    }

    public IEnumerator CoSave()
    {

        // jsonform 데이터 제작
        jsonScoreData.SceneName = SceneManager.GetActiveScene().name;
        jsonScoreData.score_total_Correct_Cnt = ScenarioManager.inst.score_totalCorrect;
        jsonScoreData.score_total_inCorrect_Cnt = ScenarioManager.inst.score_totalinCorrect;
        jsonScoreData.score_total_Cnt = ScenarioManager.inst.score_Totalcnt;
        jsonScoreData.score_Percentage = ScenarioManager.inst.score_Totalcnt > 0 ? 
            (float)ScenarioManager.inst.score_totalCorrect / ScenarioManager.inst.score_Totalcnt * 100 : 0f;

        string scoreJson = JsonUtility.ToJson(jsonScoreData, true);

        string id = DataManager.inst.userID;
        string name = DataManager.inst.userName;

        WWWForm form = new WWWForm();
        form.AddField("order", "save");
        form.AddField("id", id);
        form.AddField("name", name);
        form.AddField("scenario", SceneManager.GetActiveScene().name);
        form.AddField("date", DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss"));
        form.AddField("score", scoreJson);
        form.AddField("totalTime", ScenarioManager.inst.totalTime);
        

        yield return StartCoroutine(Post(form));

        // 폼 초기화
        jsonScoreData = new JsonScoreData();
    }

    IEnumerator Post(WWWForm form)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(URL, form))
        {
            yield return www.SendWebRequest();

            if (www.isDone) Response(www.downloadHandler.text);
            else print("웹 응답 없음");
        }
    }

    void Response(string json)
    {
        print(json);

        if (string.IsNullOrEmpty(json)) return;

        GD = JsonUtility.FromJson<GoogleData>(json);

        if (GD.result == "ERROR")
        {
            print(GD.order + " 을 실행할 수 없습니다. 에러 메세지 : " + GD.msg);
            return;
        }

        if (GD.result == "OK")
        {
            print(GD.order + " 을 실행했습니다. 메세지 : " + GD.msg);
        }
    }

    public void Add_ScoreSaveForm(List<SubmitForm> userSubmitForm, List<string> ai_Answer, ChatResponse ai_Response, string sesstionName)
    {   
        ScoreSaveForm scoreSaveForm = new ScoreSaveForm();

        scoreSaveForm.sesstionName = sesstionName;
        scoreSaveForm.score_Correct_Cnt = ai_Response.correct_count;
        scoreSaveForm.score_InCorrect_Cnt = ai_Response.incorrect_count;
        scoreSaveForm.score_Total_Cnt = ai_Response.total_questions;
        scoreSaveForm.score_Percentage = ai_Response.score_percentage;

        for (int i = 0; i < userSubmitForm.Count; i++)
        {
            QustionAndisCorrect qAndIC = new QustionAndisCorrect();
            qAndIC.question = userSubmitForm[i].txt_Question;
            qAndIC.userAnswer = userSubmitForm[i].txt_userAnswer;
            qAndIC.ai_Response = ai_Answer[i];

            string aiResp = ai_Answer[i];

            int idxCorrect = aiResp.IndexOf("정답");
            int idxWrong = aiResp.IndexOf("오답");

            if (idxCorrect == -1 && idxWrong == -1)
            {
                qAndIC.isCorrect = "?"; // 둘 다 없음
            }
            else if (idxCorrect != -1 && (idxWrong == -1 || idxCorrect < idxWrong))
            {
                qAndIC.isCorrect = "O"; // "정답"이 먼저 등장
            }
            else if (idxWrong != -1 && (idxCorrect == -1 || idxWrong < idxCorrect))
            {
                qAndIC.isCorrect = "X"; // "오답"이 먼저 등장
            }

            scoreSaveForm.ls_questionAndisCorrect.Add(qAndIC);
        }



        jsonScoreData.ls_scoreSaveForm.Add(scoreSaveForm);

    }


    [System.Serializable]
    public class GoogleData
    {
        public string order, result, msg;
        public string _id;
        public string _name;
        public string _scenario;
        public string _date;
        public string _score;
        public string _totalTime;
    }

    

    [System.Serializable]
    public class QustionAndisCorrect
    {
        public string question = "";
        public string userAnswer = "";
        public string ai_Response = "";
        public string isCorrect = "";
    }

    [System.Serializable]
    public class ScoreSaveForm
    {
        public string sesstionName = "";
        public int score_Correct_Cnt = 0;
        public int score_InCorrect_Cnt = 0;
        public int score_Total_Cnt = 0;
        public float score_Percentage = 0f;
        public List<QustionAndisCorrect> ls_questionAndisCorrect = new List<QustionAndisCorrect>();   
    }

    [System.Serializable]
    public class JsonScoreData
    {
        public string SceneName = "";
        public int score_total_Correct_Cnt = 0;
        public int score_total_inCorrect_Cnt = 0;
        public int score_total_Cnt = 0;
        public float score_Percentage = 0f;
        public List<ScoreSaveForm> ls_scoreSaveForm = new List<ScoreSaveForm>();
    }
}
