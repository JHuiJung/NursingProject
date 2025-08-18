using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Result_SessionCard : MonoBehaviour
{

    public TMP_Text Txt_Title;
    public TMP_Text Txt_O;
    public TMP_Text Txt_X;
    public TMP_Text Txt_Rate;

    public void SetCard(string title, int o, int x, float rate)
    {
        Txt_Title.text = $"{title} 결과";
        Txt_O.text = $"{o}개";
        Txt_X.text = $"{x}개";
        Txt_Rate.text = $"정답률: {rate:F1}%";
    }
}
