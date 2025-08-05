using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FeedBackCard : MonoBehaviour
{
    public TMP_Text txt_Title;
    public TMP_Text txt_UserAnswer;
    public TMP_Text txt_AiAnswer;


    public void Setup(string title, string userAnswer, string aiAnswer)
    {
        txt_AiAnswer.text = aiAnswer;
        txt_Title.text = title;
        txt_UserAnswer.text = userAnswer;
    }
}
