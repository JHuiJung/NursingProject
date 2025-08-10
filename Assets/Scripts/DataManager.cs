using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager inst;

    public string userName = "홍길동";
    public string userID = "000000";

    public string time = "9am";
    public string patient_Information = "";

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
    }

    // 셋업
    public void SetupUserInfo(string _name, string _id)
    {
        userName = _name;
        userID = _id;
    }
}
