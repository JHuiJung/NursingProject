using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class BasketEntity : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler
{
    public string entity_Title = "";

    public bool isCheck = false;

    // ¥Â∆Æ¿© Ω√∞£
    [SerializeField] float DG_Time = 0.125f;

    // ¥Â∆Æ¿© ease
    [SerializeField] Ease DG_ease = Ease.InOutQuad;

    // ¥Â∆Æ¿© ≈∏∞Ÿ∫§≈Õ
    [SerializeField] Vector3 DG_TargetScale = Vector3.one * 1.1f;

    [SerializeField, Space(10), Header("√º≈© «•Ω√")] 
    GameObject Obj_Check;

    private RectTransform rect;
    private Canvas canvas;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnOffCheck(bool _isCheck)
    {
        Obj_Check.SetActive(_isCheck);
        isCheck = _isCheck;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        this.transform.SetAsLastSibling();
        rect.DOScale(DG_TargetScale, DG_Time).SetEase(DG_ease);
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
