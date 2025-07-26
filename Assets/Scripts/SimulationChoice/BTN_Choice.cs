using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class BTN_Choice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("제출할 시뮬레이션 선택"),Space(10)]
    public Simulation_Choice simulation_Choice;
    public Simulation_ChoiceRandImg simulation_ChoiceRand = null;

    [Header("버튼 정보"), Space(10)]
    public int BTN_Number = 0;

    public string TXT_Answer = "";
    public TMPro.TMP_Text Txt_TMP;
    public Image imgNumber;

    [Header("닷 트윈 정보"), Space(10)]
    public Ease DG_Ease = Ease.InOutQuad;
    public float DG_Time = 0.15f;

    RectTransform rect;
    

    private void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(simulation_Choice != null)
        {
            if (simulation_Choice.isSimulationEnd) return;

            float endX = simulation_Choice.DG_BTN_EndX;

            rect.DOAnchorPos(new Vector2(endX - 50f, rect.anchoredPosition.y), DG_Time)
                    .SetEase(DG_Ease);

            imgNumber.color = Color.green;
        }
        else if(simulation_ChoiceRand != null)
        {
            if (simulation_ChoiceRand.isSimulationEnd) return;

            float endX = simulation_ChoiceRand.DG_BTN_EndX;

            rect.DOAnchorPos(new Vector2(endX - 50f, rect.anchoredPosition.y), DG_Time)
                    .SetEase(DG_Ease);

            imgNumber.color = Color.green;
        }
        

    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (simulation_Choice != null)
        {
            if (simulation_Choice.isSimulationEnd) return;

            float endX = simulation_Choice.DG_BTN_EndX;

            rect.DOAnchorPos(new Vector2(endX, rect.anchoredPosition.y), DG_Time)
                    .SetEase(DG_Ease);

            imgNumber.color = Color.white;
        }
        else if (simulation_ChoiceRand != null)
        {
            if (simulation_ChoiceRand.isSimulationEnd) return;

            float endX = simulation_ChoiceRand.DG_BTN_EndX;

            rect.DOAnchorPos(new Vector2(endX, rect.anchoredPosition.y), DG_Time)
                    .SetEase(DG_Ease);

            imgNumber.color = Color.white;
        }
    }

    public void SetBTN(string _content, int _num, Simulation_Choice SC)
    {
        if (simulation_Choice.isSimulationEnd) return;

        BTN_Number = _num;
        TXT_Answer = _content;
        Txt_TMP.text = TXT_Answer;
        simulation_Choice = SC;
    }

    public void SetBTN_RandImg(string _content, int _num, Simulation_ChoiceRandImg SC_RI)
    {
        BTN_Number = _num;
        TXT_Answer = _content;
        if (TXT_Answer != "")
            Txt_TMP.text = TXT_Answer;
        else
            TXT_Answer = Txt_TMP.text; // 제출할때 _content가 비워 있으면 버튼에 표시된 텍스트 반환

            simulation_ChoiceRand = SC_RI;
    }

    public void Submit()
    {
        if (simulation_Choice.isSimulationEnd) return;

        simulation_Choice.SubmitAnswer(TXT_Answer, BTN_Number);
    }

    public void Submit_RandImg()
    {
        if (simulation_ChoiceRand.isSimulationEnd) return;

        simulation_ChoiceRand.SubmitAnswer(TXT_Answer, BTN_Number);
    }

}
