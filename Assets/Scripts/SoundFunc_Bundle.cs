using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DarkTonic.MasterAudio;

public class SoundFunc_Bundle : MonoBehaviour
{
    public void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName))
        {
            Debug.LogWarning("Sound name is null or empty.");
            return;
        }
        // Play the sound using Master Audio
        MasterAudio.PlaySound(soundName);
    }
}
