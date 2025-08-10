using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
public class ConvBox : MonoBehaviour
{
    public TMP_Text txt_Name;
    public TMP_Text txt_Content;

    public static float DG_Yoffset = 175f; 

    public void Setup(string _name, string _content)
    {
        txt_Name.text = _name;
        txt_Content.text = _content;
    }

    public void MoveUp(float _time)
    {
        RectTransform rect = GetComponent<RectTransform>();

        Vector2 pos = rect.anchoredPosition;

        rect.DOAnchorPos(pos + Vector2.up * DG_Yoffset, _time).SetEase(Ease.OutQuad);
    }
}
