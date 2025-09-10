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
    RenderTexture renderTexture;

    public void CoverOnOff(bool isCoverOn)
    {
        Area_Cover.SetActive(isCoverOn);
    }

    public void Setup(string title, string userAnswer, string aiAnswer, string videoName = "", Sprite imgSprite = null)
    {
        txt_AiAnswer.text = aiAnswer;
        txt_Title.text = title;
        txt_UserAnswer.text = userAnswer;

        if (videoName != "")
        {
            renderTexture = videoPlayer.targetTexture;
            print($"{renderTexture.name}");

#if UNITY_WEBGL && !UNITY_EDITOR
            videoPath = Application.absoluteURL.Replace("index.html", "") + "videos/" + videoName + ".mp4";
#else
            videoPath = Application.streamingAssetsPath + "/" + "SampleVideo2" + ".mp4";
#endif


            print($"{name}_URL => {videoPath}");
        }

        if(imgSprite != null)
        {
            img_AnswerImg.sprite = imgSprite;
        }

    }

    public void ClearRenderTexture()
    {
        RenderTexture activeRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(true, true, Color.black); // 검은색으로 초기화
        RenderTexture.active = activeRT;
    }

    public void PlayVideo()
    {
        if (videoPlayer != null && videoPath != "")
        {
            DataManager.inst.SetMute(true);

            MasterAudio.PlaySound("Button_Press");
            videoPlayer.url = videoPath;
            videoPlayer.SetDirectAudioVolume(0, 1f); // Mute audio
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
            DataManager.inst.SetMute(false);

            MasterAudio.PlaySound("Button_Press");
            videoPlayer.Stop();
        }
    }
}
