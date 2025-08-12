using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using uMicrophoneWebGL;

public class Title_UserInfo : MonoBehaviour
{
    public TMP_InputField inputField_UserName;
    public TMP_InputField inputField_UserID;
    public GameObject obj_BTN_Submit;
    public MicrophoneWebGL microphoneWebGL;
    public Dropdown deviceDropdown;

    private void Start()
    {
        microphoneWebGL.RefreshDeviceList();
    }

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

    public void OnDeviceListUpdated(List<Device> devices)
    {
        if (!deviceDropdown) return;
        print("hi");
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
    }

}
