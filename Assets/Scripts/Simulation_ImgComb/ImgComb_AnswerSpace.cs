using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImgComb_AnswerSpace : MonoBehaviour
{

    public bool isFilled = false;

    public float findingRange = 120;

    public string number = "1";

    public GameObject Obj_ImgCombEntites;

    List<ImgComb_Entity> imgComb_Entities = new List<ImgComb_Entity>();

    RectTransform rect;
    private void Start()
    {
        rect = GetComponent<RectTransform>();
        Setup();
    }

    void Setup()
    {
        for (int i = 0; i < Obj_ImgCombEntites.transform.childCount; i++)
        {
            ImgComb_Entity tmp = Obj_ImgCombEntites.transform.GetChild(i).GetComponent<ImgComb_Entity>();

            if (tmp != null)
            {
                imgComb_Entities.Add(tmp);
            }
        }
    }

    public void CheckFind()
    {

        List<ImgComb_Entity> dummy = new List<ImgComb_Entity>();

        int cnt = 0;

        // 가장 가까운 엔티티 찾기
        foreach (ImgComb_Entity entity in imgComb_Entities)
        {
            float distance = Vector2.Distance(
                rect.anchoredPosition,
                entity.gameObject.GetComponent<RectTransform>().anchoredPosition
            );

            if (distance <= findingRange)
            {
                entity.OnNumber(number);
                cnt++;
            }
        }

        if (cnt > 0) {
            isFilled = true;
        }
        else
        {
            isFilled = false;
        }
    }



}
