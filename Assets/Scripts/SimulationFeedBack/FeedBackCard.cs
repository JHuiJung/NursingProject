using DarkTonic.MasterAudio;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class FeedBackCard : MonoBehaviour
{
    public GameObject Area_Cover;
    public TMP_Text txt_Title;
    public TMP_Text txt_UserAnswer;
    public TMP_Text txt_AiAnswer;
    public VideoPlayer videoPlayer;
    public Image img_AnswerImg;

    string videoPath = "";

    public void CoverOnOff(bool isCoverOn)
    {
        Area_Cover.SetActive(isCoverOn);
    }

    public void Setup(string title, string userAnswer, string aiAnswer, string videoName = "", Sprite imgSprite = null)
    {
        txt_AiAnswer.text = aiAnswer;
        txt_Title.text = title;
        txt_UserAnswer.text = userAnswer;

        if(videoName != "")
        {
            videoPath = videoPath = Application.streamingAssetsPath + "/" + videoName + ".mp4";
        }

        if(imgSprite != null)
        {
            img_AnswerImg.sprite = imgSprite;
        }

    }

    public void PlayVideo()
    {
        if (videoPlayer != null && videoPath != "")
        {
            MasterAudio.PlaySound("Button_Press");
            videoPlayer.url = videoPath;
            videoPlayer.SetDirectAudioVolume(0, 0f); // Mute audio
            videoPlayer.Play();
        }
        else
        {
            Debug.Log("비디오 실행 오류");
        }
    }

    public void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            MasterAudio.PlaySound("Button_Press");
            videoPlayer.Stop();
        }
    }
}
