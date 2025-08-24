using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using TMPro;
using DarkTonic.MasterAudio;

public class ImgComb_Entity : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler
{

    public string entity_Title = "";

    public int number = -1;

    public bool isBTNOn = false;

    // ¥Â∆Æ¿© Ω√∞£
    [SerializeField] float DG_Time = 0.125f;

    // ¥Â∆Æ¿© ease
    [SerializeField] Ease DG_ease = Ease.Linear;

    // ¥Â∆Æ¿© ≈∏∞Ÿ∫§≈Õ
    [SerializeField] Vector3 DG_TargetScale = Vector3.one;

    [SerializeField, Space(10), Header("º˝¿⁄ «•Ω√")] GameObject Obj_Num;
    [SerializeField] TMP_Text txt_Num;

    [Header("Card Info"), Space(10)]
    public GameObject obj_Info;

    private RectTransform rect;
    private Canvas canvas;

    private void Start()
    {
        rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        if(obj_Info != null)
        {
            obj_Info.SetActive(false);
        }
    }

    public void OnNumber(int num)
    {
        Obj_Num.SetActive(true);
        number = num;
        txt_Num.text = number.ToString();
        isBTNOn = true;
    }

    public void OffNumber()
    {
        Obj_Num.SetActive(false);
        number = -1;
        txt_Num.text = number.ToString();
        isBTNOn = false;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        MasterAudio.PlaySound("Button_Hover");
        this.transform.SetAsLastSibling();
        if (obj_Info != null)
        {
            obj_Info.SetActive(true);
        }
        
        rect.DOScale( DG_TargetScale, DG_Time).SetEase( DG_ease );
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (obj_Info != null)
        {
            obj_Info.SetActive(false);
        }
        
        rect.DOScale(Vector3.one, DG_Time).SetEase(DG_ease);
    }

    public void OnDrag(PointerEventData eventData)
    {
        rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
}
