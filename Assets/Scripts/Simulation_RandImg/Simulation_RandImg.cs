using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class SpriteAndTextAnswer
{
    public Sprite sprite;
    public string text_Content;
    public string text_Answer;
}
public class Simulation_RandImg : MonoBehaviour
{

    // 더미 뭉치
    [Header("답 더미 뭉치"), Space(10)]
    public List<SpriteAndTextAnswer> spriteAndTextAnswer_Dummies = new List<SpriteAndTextAnswer>();
    
    // 타겟 이미지
    [Header("Obj 이미지"), Space(10)]
    public Image Obj_targetImg;

    public SpriteAndTextAnswer selectedTA;

    private void OnEnable()
    {
        Setup();
    }

    public void Setup()
    {
        selectedTA = spriteAndTextAnswer_Dummies[Random.Range(0, spriteAndTextAnswer_Dummies.Count)];
        Obj_targetImg.sprite = selectedTA.sprite;
    }

    public void ResetRI()
    {
        selectedTA = null;
    }
}
