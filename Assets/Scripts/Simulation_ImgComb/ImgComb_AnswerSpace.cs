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
        ImgComb_Entity closestEntity = null;
        List<ImgComb_Entity> dummy = new List<ImgComb_Entity>();
        float closestDistance = float.MaxValue;

        // 가장 가까운 엔티티 찾기
        foreach (ImgComb_Entity entity in imgComb_Entities)
        {
            float distance = Vector2.Distance(
                rect.anchoredPosition,
                entity.gameObject.GetComponent<RectTransform>().anchoredPosition
            );

            if (distance <= findingRange)
            {
                dummy.Add(entity);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestEntity = entity;
                }

            }
        }

        // 각 엔티티마다 On/Off 처리
        foreach (ImgComb_Entity entity in dummy)
        {
            if (entity == closestEntity)
            {
                entity.OnNumber(number); // 가장 가까운 애만 On
            }
            else
            {
                entity.OffNumber(); // 나머지는 Off
            }
        }

        if (closestEntity != null)
        {
            isFilled = true;
        }
        else
        {
            isFilled = false;
        }
    }



}
