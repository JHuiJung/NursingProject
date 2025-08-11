using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Title_UserInfo : MonoBehaviour
{
    public TMP_InputField inputField_UserName;
    public TMP_InputField inputField_UserID;
    public GameObject obj_BTN_Submit;

    private void Update()
    {
        if(string.IsNullOrEmpty(inputField_UserName.text) ||
            string.IsNullOrEmpty(inputField_UserID.text))
        {
            obj_BTN_Submit.SetActive(false);
        }
        else
        {
            obj_BTN_Submit.SetActive(true);
        }
        
    }

    public void Submit()
    {
        string _name = inputField_UserName.text;
        string _id = inputField_UserID.text;

        DataManager.inst.SetupUserInfo( _name, _id );
    }



}
