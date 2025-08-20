using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using uMicrophoneWebGL;

public class TEST_Mic : MonoBehaviour
{
    public Dropdown deviceDropdown;

    bool isMrocording = false;

    private void LateUpdate()
    {
        OnDeviceListUpdated();
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

    #region ----------------------------------------STT 
    public void ToggleRecord()
    {

        isMrocording = STT_TTS_Manager.inst.isMRecording;

        if (!isMrocording)
        {
            // recording - begin

        }
        else
        {
            // no Recording - end
        }

        STT_TTS_Manager.inst.ToggleRecord();
    }

    public void PlayAudio()
    {
        STT_TTS_Manager.inst.TogglePlay();
    }

    #endregion

}
