using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

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
public class DataManager : MonoBehaviour
{
    public static DataManager inst;

    public string userName = "홍길동";
    public string userID = "000000";

    public string patient_Information = "";

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
        string id = DataManager.inst.userID;
        string name = DataManager.inst.userName;

        WWWForm form = new WWWForm();
        form.AddField("order", "save");
        form.AddField("id", id);
        form.AddField("name", name);
        form.AddField("scenario", SceneManager.GetActiveScene().name);
        form.AddField("date", DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss"));
        form.AddField("score", ScenarioManager.inst.score);
        form.AddField("totalTime", ScenarioManager.inst.totalTime);
        

        yield return StartCoroutine(Post(form));
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
}
