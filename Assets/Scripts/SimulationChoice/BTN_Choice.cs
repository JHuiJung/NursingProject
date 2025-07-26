using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class BTN_Choice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Simulation_Choice simulation_Choice;

    public int BTN_Number = 0;

    public string TXT_Answer = "";
    public TMPro.TMP_Text Txt_TMP;

    public Ease DG_Ease = Ease.InOutQuad;
    public float DG_Time = 0.15f;

    public Image imgNumber;

    RectTransform rect;
    private void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (simulation_Choice.isSimulationEnd) return;

        float endX = simulation_Choice.DG_BTN_EndX;

        rect.DOAnchorPos(new Vector2(endX - 50f, rect.anchoredPosition.y), DG_Time)
                .SetEase(DG_Ease);

        imgNumber.color = Color.green;

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (simulation_Choice.isSimulationEnd) return;

        float endX = simulation_Choice.DG_BTN_EndX;

        rect.DOAnchorPos(new Vector2(endX, rect.anchoredPosition.y), DG_Time)
                .SetEase(DG_Ease);

        imgNumber.color = Color.white;
    }

    public void SetBTN(string _content, int _num, Simulation_Choice SC)
    {
        if (simulation_Choice.isSimulationEnd) return;

        BTN_Number = _num;
        TXT_Answer = _content;
        Txt_TMP.text = TXT_Answer;
        simulation_Choice = SC;
    }

    public void Submit()
    {
        if (simulation_Choice.isSimulationEnd) return;

        simulation_Choice.SubmitAnswer(TXT_Answer, BTN_Number);
    }

}
