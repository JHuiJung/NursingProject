using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DarkTonic.MasterAudio;

public class Admin_UserInfo_Card : MonoBehaviour
{
    [Header("Txts")]
    public TMP_Text txt_Scenario;
    public TMP_Text txt_Date;

    [Header("Google data")]
    public DataManager.GoogleData googleData;

    Admin_Func admin_Func;

    public void Setup(DataManager.GoogleData _googleData, Admin_Func _adminFunc)
    {
        googleData = _googleData;
        admin_Func = _adminFunc;

        txt_Scenario.text = $"시나리오 : {googleData._scenario}";
        txt_Date.text = $"수행 날짜 : {googleData._date}";

        

    }

    public void OpenInfo()
    {
        MasterAudio.PlaySound("Button_Press");
        admin_Func.OpenInfo(googleData);
    }


}
