using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class UI_Prescription : MonoBehaviour, IDragHandler
{
    public Vector3 targetVec = Vector3.one;
    public float DG_Time = 0.25f;
    public Ease DG_Ease = Ease.InOutQuad;

    RectTransform rect;
    private Canvas canvas;
    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        rect.DOPunchScale(targetVec, DG_Time).SetEase(Ease.InOutQuad);
    }
    public void OnDrag(PointerEventData eventData)
    {
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    private void OnEnable()
    {
        rect.DOPunchScale(targetVec, DG_Time).SetEase(Ease.InOutQuad);
    }

}
