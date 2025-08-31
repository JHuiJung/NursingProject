using DarkTonic.MasterAudio;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ImgClick_Entity : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{

    public TMP_Text txt_Title;

    public int number = -1;

    public int clickedOrder = 999999;

    public bool isClicked = false;

    // 닷트윈 시간
    [Header("Dotween"), Space(10)]
    [SerializeField] float DG_Time = 0.125f;
    [SerializeField] Ease DG_ease = Ease.InOutQuad;
    [SerializeField] Vector3 DG_TargetScale = Vector3.one;

    [SerializeField, Space(10), Header("숫자 표시")] GameObject Obj_Num;
    public TMP_Text txt_Num;

    [Header("Card Info"), Space(10)]
    public GameObject obj_Info;
    public GameObject obj_Cancle;
    public GameObject obj_Confirm;

    private RectTransform rect;
    private Simulation_ImgClick simulation_ImgClick;

    private void Start()
    {
        rect = GetComponent<RectTransform>();

        if (obj_Info != null)
        {
            obj_Info.SetActive(false);
        }

        simulation_ImgClick = transform.parent.parent.parent.GetComponent<Simulation_ImgClick>();
    }

    public void CardReset()
    {
        isClicked = false;
        Obj_Num.SetActive(false);
        obj_Cancle.SetActive(false);
        obj_Confirm.SetActive(false);
        obj_Info.SetActive(false);

        number = -1;
        txt_Num.text = number.ToString();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MasterAudio.PlaySound("Button_Hover");

        if (obj_Info != null)
        {
            obj_Info.SetActive(true);
        }

        if (isClicked)
        {
            // 취소
            obj_Cancle.SetActive(true);
            obj_Confirm.SetActive(false);

        }
        else
        {
            // 선택
            obj_Cancle.SetActive(false);
            obj_Confirm.SetActive(true);
        }

        this.transform.SetAsLastSibling();

        rect.DOScale(DG_TargetScale, DG_Time).SetEase(DG_ease);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (obj_Info != null)
        {
            obj_Info.SetActive(false);
        }

        obj_Cancle.SetActive(false);
        obj_Confirm.SetActive(false);

        rect.DOScale(Vector3.one, DG_Time).SetEase(DG_ease);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        MasterAudio.PlaySound("Button_Click");

        if (isClicked)
        {
            // 취소
            Obj_Num.SetActive(false);
            isClicked = !isClicked;

            obj_Cancle.SetActive(false);
            obj_Confirm.SetActive(true);

            clickedOrder = 999999;
        }
        else
        {
            // 선택
            Obj_Num.SetActive(true);
            simulation_ImgClick.currentCardNumber++;
            isClicked = !isClicked;

            clickedOrder = simulation_ImgClick.currentCardNumber;

            obj_Cancle.SetActive(true);
            obj_Confirm.SetActive(false);

        }

        simulation_ImgClick.CardOrganise();

    }
}
