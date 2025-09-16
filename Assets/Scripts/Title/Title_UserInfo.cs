using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using uMicrophoneWebGL;
using DarkTonic.MasterAudio;

public class Title_UserInfo : MonoBehaviour
{
    public TMP_InputField inputField_UserName;
    public TMP_InputField inputField_UserID;
    public GameObject obj_BTN_Submit;
    public Dropdown deviceDropdown;

    public GameObject Btn_Admin;

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

    private void LateUpdate()
    {
        OnDeviceListUpdated();
    }

    public void Submit()
    {
        string _name = inputField_UserName.text;
        string _id = inputField_UserID.text;

        MasterAudio.PlaySound("Button_Press");

        DataManager.inst.SetupUserInfo( _name, _id );

        if(_name == "admin" || name == "Admin")
        {
            Btn_Admin.SetActive(true);
        }
    }

    public void OnDeviceListUpdated()
    {
        if (!deviceDropdown) return;

        List<Device> devices = STT_TTS_Manager.inst.devices;

        var options = new List<Dropdown.OptionData>();
        foreach (var device in devices)
        {
            Dropdown.OptionData option = new Dropdown.OptionData()
            {
                text = $"{device.label} (Ch: {device.channelCount})",
            };
            options.Add(option);
        }
        deviceDropdown.options = options;
    }

    public void MicChange()
    {
        STT_TTS_Manager.inst.microphoneWebGL.micIndex = deviceDropdown.value;
    }

}
