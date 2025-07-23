using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using TMPro;

public class ImgComb_Entity : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler
{
    // ¥Â∆Æ¿© Ω√∞£
    [SerializeField] float DG_Time = 0.125f;

    // ¥Â∆Æ¿© ease
    [SerializeField] Ease DG_ease = Ease.Linear;

    // ¥Â∆Æ¿© ≈∏∞Ÿ∫§≈Õ
    [SerializeField] Vector3 DG_TargetScale = Vector3.one;

    [SerializeField, Space(10), Header("º˝¿⁄ «•Ω√")] GameObject Obj_Num;
    [SerializeField] TMP_Text txt_Num;

    private RectTransform rect;
    private Canvas canvas;
    public bool isNumberOn = false;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnNumber(string num)
    {
        Obj_Num.SetActive(true);
        txt_Num.text = num;
        isNumberOn = true;
    }

    public void OffNumber()
    {
        Obj_Num.SetActive(false);
        txt_Num.text = "";
        isNumberOn = false;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        this.transform.SetAsLastSibling();
        rect.DOScale( DG_TargetScale, DG_Time).SetEase( DG_ease );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rect.DOScale(Vector3.one, DG_Time).SetEase(DG_ease);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}
