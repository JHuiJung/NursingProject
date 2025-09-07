using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class TEST : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public TMP_InputField inputField;
    // Start is called before the first frame update

    string videoPath;

    public void SetVideoUrl()
    {
        videoPath = Application.absoluteURL.Replace("index.html", "") + "videos/" + inputField.text + ".mp4";
        print($" URL => {videoPath}");
    }

    [ContextMenu("Play Video")]
    public void PlayVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.url = videoPath;
            videoPlayer.SetDirectAudioVolume(0, 0.5f); // Mute audio
            videoPlayer.Play();
        }
        else
        {
            Debug.LogError("VideoPlayer is not assigned.");
        }
    }

    [ContextMenu("Stop Video")]
    public void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
            
        }
        else
        {
            Debug.LogError("VideoPlayer is not playing or not assigned.");
        }
    }
}
