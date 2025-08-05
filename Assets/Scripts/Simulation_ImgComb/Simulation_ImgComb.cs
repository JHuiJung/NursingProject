using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Simulation_ImgComb : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10)]
    public GameObject Obj_CanvasChoice;


    [TextArea] //질문
    [Header("질문(필수로 입력)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    public List<GameObject> answerSpaces = new List<GameObject>();
    public List<ImgComb_Entity> imgComb_Entities = new List<ImgComb_Entity>();

    [SerializeField] GameObject Obj_Button;
    public float findingRange = 120;

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public Ease DG_Ease = Ease.InOutQuad;
    public float DG_deltaTime = 0.15f;

    public List<Vector2> answerSpaceTargets = new List<Vector2>();

    // 시뮬레이션 끝 bool
    private bool isSimulationEnd = false;
    private ScenarioManager _sm;
    

    string userAnswer = "";

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 시작");

        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup();
        StartCoroutine(AllUiOn());
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;



        /*
           answerSpace 보다 안에 있는 것들은 숫자를 표기하고 셋에 추가

           마지막에 셋에 없는 것들은 번호 꺼버리기
         */

        HashSet<int> collectedEntityIndices = new HashSet<int>();

        for (int i = 0; i < answerSpaces.Count; i++)
        {
            RectTransform answerRect = answerSpaces[i].GetComponent<RectTransform>();

            float closestSqrDistance = float.MaxValue;
            int closestEntityIndex = -1;

            for (int j = 0; j < imgComb_Entities.Count; j++)
            {
                if (collectedEntityIndices.Contains(j)) continue; // 이미 연결된 Entity는 제외

                RectTransform entityRect = imgComb_Entities[j].GetComponent<RectTransform>();
                float sqrDistance = (answerRect.position - entityRect.position).sqrMagnitude;

                if (sqrDistance <= findingRange * findingRange && sqrDistance < closestSqrDistance)
                {
                    closestSqrDistance = sqrDistance;
                    closestEntityIndex = j;
                }
            }

            if (closestEntityIndex != -1)
            {
                collectedEntityIndices.Add(closestEntityIndex);
                imgComb_Entities[closestEntityIndex].OnNumber(i + 1);
            }
        }

        // 연결되지 않은 Entity는 번호 꺼버리기
        for (int i = 0; i < imgComb_Entities.Count; ++i)
        {
            if (!collectedEntityIndices.Contains(i))
            {
                imgComb_Entities[i].OffNumber();
            }
        }


        // 전부다 채워짐
        if (CheckIsAllBTNOn())
        {
            Obj_Button.SetActive(true);
        }
        else
        {
            Obj_Button.SetActive(false);
        }

        
    }

    public void Submit()
    {
        isSimulationEnd = true;

        imgComb_Entities.Sort((a, b) =>
        {
            int numA = a.number;
            int numB = b.number;
            return numA.CompareTo(numB);
        });


        userAnswer += text_Question + "/ User Answer : ";


        for (int i = 0; i < imgComb_Entities.Count; i++) {

            ImgComb_Entity e = imgComb_Entities[i];

            if (!e.isBTNOn) continue;

            userAnswer += $"[ {e.number}번 : {e.entity_Title}]";

            if(i != imgComb_Entities.Count - 1)
            {
                userAnswer += " -> ";
            }
            else
            {
                userAnswer += "\n";
            }


        }

        print($"{name} : {userAnswer}");

        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = "미리 제공된 답변 참고";
        submitForm.txt_userAnswer = userAnswer;

        _sm.str_Answers.Push(submitForm);

        StartCoroutine(AllUiOff());

    }


    bool CheckIsAllBTNOn()
    {
        int cnt = 0;

        foreach (ImgComb_Entity entity in imgComb_Entities)
        {
            if (entity.isBTNOn)
                cnt++;
        }
        
        if (cnt == answerSpaces.Count)
            return true;
        else
            return false;
    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 객관식 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
        userAnswer = "";
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;
    }

    //------------------------------------------------------------------------------------------

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // 정답란 닷트윈
        for (int i = 0; i < answerSpaceTargets.Count; i++) 
        {
            answerSpaces[i].GetComponent<RectTransform>().DOAnchorPos(
                answerSpaceTargets[i], DG_deltaTime * (i+1)).SetEase(DG_Ease);
        }

        // 엔티티 카드 섞기
        for (int i = 0; i < imgComb_Entities.Count; i++)
        {
            int randomIndex = UnityEngine.Random.Range(i, imgComb_Entities.Count);
            ImgComb_Entity temp = imgComb_Entities[i];
            imgComb_Entities[i] = imgComb_Entities[randomIndex];
            imgComb_Entities[randomIndex] = temp;
        }

        // 엔티티 카드 닷트윈
        for (int i = 0; i < imgComb_Entities.Count / 2; i++)
        {
            Vector2 pos = new Vector2(UnityEngine.Random.Range(-600, -700), UnityEngine.Random.Range(-200, 200));

            imgComb_Entities[i].GetComponent<RectTransform>().DOAnchorPos(
                pos, DG_deltaTime * (i + 1)).SetEase(DG_Ease);
        }

        for (int i = imgComb_Entities.Count / 2; i < imgComb_Entities.Count ; i++)
        {
            Vector2 pos = new Vector2(UnityEngine.Random.Range(600, 700), UnityEngine.Random.Range(-200, 200));

            imgComb_Entities[i].GetComponent<RectTransform>().DOAnchorPos(
                pos, DG_deltaTime * (i + 1)).SetEase(DG_Ease);
        }

        yield return new WaitForSeconds(DG_deltaTime * imgComb_Entities.Count);
    }

    IEnumerator AllUiOff()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        // 정답란 닷트윈
        for (int i = 0; i < answerSpaceTargets.Count; i++)
        {
            answerSpaces[i].GetComponent<RectTransform>().DOAnchorPos(
                new Vector2(0, -800f), DG_deltaTime * (i + 1)).SetEase(DG_Ease);
        }

        // 엔티티 카드 닷트윈
        for (int i = 0; i < imgComb_Entities.Count; i++)
        {
            imgComb_Entities[i].GetComponent<RectTransform>().DOAnchorPos(
                new Vector2(0, 800f), DG_deltaTime * (i + 1)).SetEase(DG_Ease);
        }


        yield return new WaitForSeconds(DG_deltaTime * imgComb_Entities.Count);

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();
    }

    void Setup()
    {
        // 질문 텍스트 수정
        Tmp_Question.text = text_Question;

        
    }
}
