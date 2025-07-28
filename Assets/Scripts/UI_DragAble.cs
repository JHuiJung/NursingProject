using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;

public class UI_DragAble : MonoBehaviour, IDragHandler
{
    [Header("Dotween ¿É¼Ç"),Space(10)]
    public bool useDotweenFX = false;
    public Vector3 targetVec = new Vector3(0.1f,0.1f,0.1f);
    public float DG_Time = 0.12f;
    public Ease DG_Ease = Ease.InOutQuad;

    RectTransform rect;
    private Canvas canvas;
    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if(useDotweenFX)
            rect.DOPunchScale(targetVec, DG_Time).SetEase(Ease.InOutQuad);
    }
    public void OnDrag(PointerEventData eventData)
    {
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    private void OnEnable()
    {
        if (useDotweenFX)
            rect.DOPunchScale(targetVec, DG_Time).SetEase(Ease.InOutQuad);
    }

}
