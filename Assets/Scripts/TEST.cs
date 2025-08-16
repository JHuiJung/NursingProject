using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TEST : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string videoTitle = "SampleVideo";
    // Start is called before the first frame update

    string videoPath;

    void Start()
    {
        //videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, $"{videoTitle}.mp4");
        videoPath = videoPath = Application.streamingAssetsPath + "/" + videoTitle + ".mp4";
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
