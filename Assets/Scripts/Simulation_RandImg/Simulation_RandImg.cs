using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public class SpriteAndTextAnswer
{
    public Sprite sprite;
    public List<string> ls_Answers;
}
public class Simulation_RandImg : MonoBehaviour
{

    // 더미 뭉치
    [Header("답 더미 뭉치"), Space(10)]
    public List<SpriteAndTextAnswer> spriteAndTextAnswer_Dummies = new List<SpriteAndTextAnswer>();
    
    // 타겟 이미지
    [Header("Obj 이미지"), Space(10)]
    public Image Obj_targetImg;
    public GameObject obj_AreaImage;

    public SpriteAndTextAnswer selectedTA;

    [Header("환자 상태 갱신"), Space(10)]
    public bool isUpdatePatientInfo = false;
    public int targetIndex = 0;

    public void Setup()
    {
        selectedTA = spriteAndTextAnswer_Dummies[Random.Range(0, spriteAndTextAnswer_Dummies.Count)];
        Obj_targetImg.sprite = selectedTA.sprite;

        if(isUpdatePatientInfo)
        {
            DataManager.inst.patient_Information = $"{selectedTA.ls_Answers[targetIndex]}";
        }
    }

    public void ResetRI()
    {
        selectedTA = null;
    }

    public void UiOn()
    {
        obj_AreaImage.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, 0), 0.75f).SetEase(Ease.InOutQuad);
    }

    public void UiOff()
    {
        obj_AreaImage.GetComponent<RectTransform>().DOAnchorPos(new Vector2(0, -800f), 0.75f).SetEase(Ease.InOutQuad);
    }


}
