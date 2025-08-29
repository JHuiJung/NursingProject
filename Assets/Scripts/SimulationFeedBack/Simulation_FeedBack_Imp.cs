using DarkTonic.MasterAudio;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static DataManager;
using static NursingChatClient;


public class Simulation_FeedBack_Imp : SimulationBase
{
    public NursingChatClient NursingChatClient;
    public ChatResponse aiResponse = null;

    [Header("Canvas Obj & Stuff"), Space(10),SerializeField]
    GameObject Obj_CanvasChoice;
    [SerializeField]
    GameObject Obj_Title;
    [SerializeField]
    GameObject Obj_Content;
    [SerializeField]
    GameObject Obj_Wait;
    

    [Header("Answer Cards"), Space(10)]
    public GameObject obj_Area_Cards;
    public GameObject obj_Area_BTns;
    public GameObject pf_FeedbackCard;
    public GameObject pf_FeedbackResultCard;
    public GameObject pf_FeedbackVideoCard;
    public List<GameObject> list_FeedbackCards = new List<GameObject>();
    public int currentCardNum = 0;
    [SerializeField]
    TMP_Text txt_PageNum;

    [Header("Pass or NonPass"), Space(10)]
    public string sessionName = "Session 1";
    public float pass_Threshold = 0f;
    public GameObject obj_Pass_Package;
    

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_TimeDelta = 0.2f;
    public Ease DG_Ease = Ease.InOutQuad;

    List<string> ls_isCorrect = new List<string>();
    List<string> ls_Response = new List<string>();
    bool isSimulationEnd = false;
    ScenarioManager _sm;
    public override void Enter(ScenarioManager SM)
    {
        _sm = SM;
        aiResponse = new ChatResponse();

        Obj_CanvasChoice.SetActive(true);

        StartCoroutine(Setup());
    }

    public override void Excute(ScenarioManager SM)
    {
        if(isSimulationEnd) return;

        //나중에 지워야함
        if(Input.GetKeyDown(KeyCode.P))
        {
            Pass();
        }
    }

    public override void Exit(ScenarioManager SM)
    {
        isSimulationEnd = false;

        // ȭ�� ����
        Obj_CanvasChoice.SetActive(false);
        StartCoroutine(AllUIOff());
        ResetSimulation();
    }

    [ContextMenu("Pass")]
    public void Pass()
    {
        MasterAudio.PlaySound("Button_Press");

        print($"{sessionName} 이 통과(Pass) 됨");

        // 패스 패키지 생성
        if( obj_Pass_Package != null)
        {
            var Obj_passPackage = Instantiate(obj_Pass_Package, transform.parent);
            SimulationBase simulationBase = Obj_passPackage.GetComponent<SimulationBase>();
            _sm.simulationBases.Add(simulationBase);
            Obj_passPackage.transform.SetAsLastSibling();
        }

        _sm.NextSimulation();
    }

    [ContextMenu("NonPass")]
    public void NonPass()
    {
        MasterAudio.PlaySound("Button_Press");

        print($"{sessionName} 이 불통과(NonPass) 됨");
        _sm.NextSimulation();
        
    }

    public override void ResetSimulation()
    {
        isSimulationEnd = false;
        aiResponse = null;

        // 카드 역순으로 삭제해야 안전
        list_FeedbackCards.Clear();

        // 리스트 초기화
        ls_isCorrect.Clear();
        ls_Response.Clear();

        for (int i = obj_Area_Cards.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(obj_Area_Cards.transform.GetChild(i).gameObject);
        }

        //%%%%%%%%%%%%%%%%%%%% �ӽ÷� �ǵ�� ����� ���� ���� %%%%%%%%%%%%%%%%%%%%%%
        _sm.str_Answers.Clear();
    }

    IEnumerator Setup()
    {
        Obj_Wait.SetActive(true);
        obj_Area_BTns.SetActive(false);
        Obj_CanvasChoice.SetActive(true);

        yield return new WaitForSeconds(0.2f);

        List<SubmitForm> userStack = new List<SubmitForm>(_sm.str_Answers);

        List<FeedBackCardForm> feedbackCardForms = new List<FeedBackCardForm>();

        // 답변 뭉치 초기화
        ls_isCorrect.Clear();
        ls_Response.Clear();

        // 1. ai 답변 카드 만들기

        string _result_For_AIAnswer = "";

        for (int i = 0; i < userStack.Count; i++)
        {
            SubmitForm submitForm = userStack[i];
            
            if(submitForm.useAiAnswer)
            {
                FeedBackCardForm aiCardForm = new FeedBackCardForm();
                aiCardForm.index = i;
                aiCardForm.submitForm = submitForm;

                _result_For_AIAnswer += "\n";
                _result_For_AIAnswer += $"{submitForm.txt_Question} / QuestionAnswer : {submitForm.txt_QuestionAnswer} / UserAnswer : {submitForm.txt_userAnswer} ";
                _result_For_AIAnswer += "\n--------------------------------";

                feedbackCardForms.Add(aiCardForm);
            }
        }

        // ai를 통해 정보 가져오기
        if (_result_For_AIAnswer != "")
        {
            print($"{name} : AI에게 질문을 보냄 : {_result_For_AIAnswer}");
            yield return StartCoroutine(NursingChatClient.SendQuestionToAPIUsing(_result_For_AIAnswer));

            // 답변 리스트 반환
            List<string> aianswers = GetQList(aiResponse.answer);

            // 카드 폼에 답변 추가
            for (int i = 0; i < aianswers.Count; i++)
            {
                string aiResp = aianswers[i];

                int idxCorrect = aiResp.IndexOf("정답");
                int idxWrong = aiResp.IndexOf("오답");

                string isCorrect = "X"; // 기본값은 오답

                if (idxCorrect == -1 && idxWrong == -1)
                {
                    isCorrect = "?"; // 둘 다 없음
                }
                else if (idxCorrect != -1 && (idxWrong == -1 || idxCorrect < idxWrong))
                {
                    isCorrect = "O"; // "정답"이 먼저 등장
                    aiResp = $"<color=#BEFFA3>정답 : </color> {aiResp}";
                }
                else if (idxWrong != -1 && (idxCorrect == -1 || idxWrong < idxCorrect))
                {
                    isCorrect = "X"; // "오답"이 먼저 등장
                    aiResp = $"<color=#FF7A7A>오답 : </color> {aiResp}";
                }

                feedbackCardForms[i].display_Answer = aiResp;
                feedbackCardForms[i].origin_Answer = aianswers[i];
                feedbackCardForms[i].isCorrect = isCorrect;
            }
        }

        // 2. 일반 카드 생성

        for (int i = 0; i < userStack.Count; i++)
        {
            SubmitForm submitForm = userStack[i];

            if (!submitForm.useAiAnswer)
            {
                // 일반 카드 폼 생성
                FeedBackCardForm normalCardForm = new FeedBackCardForm();
                normalCardForm.index = i;
                normalCardForm.submitForm = submitForm;

                // 시나리오 dlfma, 문제 번호, 사용자의 대답 매개변수로 해 유저에 대답에 대한 답변 받아오기 (데이터 뱅크)
                string sceneName = SceneManager.GetActiveScene().name;
                string normalAnswer = DataManager.inst.GetNormalResponse(sceneName, normalCardForm.submitForm.quiz_index,
                    normalCardForm.submitForm.txt_userAnswer, normalCardForm.submitForm.txt_QuestionAnswer);

                print($"{name} : index : {normalCardForm.index} / Question : {normalCardForm.submitForm.txt_Question} / UserAnswer : {normalCardForm.submitForm.txt_userAnswer} / NormalAnswer : {normalAnswer}");

                normalCardForm.origin_Answer = normalAnswer;

                // 답변이 맞았으면 aiResponse의 개수 변경  
                if (normalCardForm.submitForm.txt_userAnswer
                    == normalCardForm.submitForm.txt_QuestionAnswer)
                {
                    aiResponse.correct_count++;
                    // 카드 폼 답변란 추가
                    normalCardForm.display_Answer = "<color=#BEFFA3>정답 : </color>" + normalAnswer;
                    normalCardForm.isCorrect = "O"; // 정답
                }
                else
                {
                    aiResponse.incorrect_count++;
                    // 카드 폼 답변란 추가
                    normalCardForm.display_Answer = "<color=#FF7A7A>오답 : </color>" + normalAnswer;
                    normalCardForm.isCorrect = "X";
                }

                aiResponse.total_questions++;
                feedbackCardForms.Add(normalCardForm);
            }
        }

        // 3. 만들어진 카드들 인덱스를 기준으로 정렬 후 list_FeedbackCards에 추가
        feedbackCardForms.Sort((a, b) => a.index.CompareTo(b.index));

        // 카드 생성
        for(int i = 0; i < feedbackCardForms.Count; i++)
        {
            FeedBackCardForm cardForm = feedbackCardForms[i];
            MakeAnswerCard(cardForm);
        }

        // 섹션 정답률 업데이트
        if (aiResponse.total_questions > 0)
        {
            aiResponse.score_percentage = (float)aiResponse.correct_count / aiResponse.total_questions * 100f;
        }
        else
        {
            aiResponse.score_percentage = 0f;
        }

        //qentences 업데이트
        for(int i = 0; i < feedbackCardForms.Count; i++)
        {
            ls_Response.Add(feedbackCardForms[i].origin_Answer);
            ls_isCorrect.Add(feedbackCardForms[i].isCorrect);
        }

        // 4. 결과 카드 추가
        var np2 = Instantiate(pf_FeedbackResultCard, obj_Area_Cards.transform);
        np2.name = $"pf_FeedBackResultCard";
        FeedBackResultCard feedBackResultCard = np2.GetComponent<FeedBackResultCard>();

        feedBackResultCard.Setup(pass_Threshold, aiResponse);

        list_FeedbackCards.Add(np2);


        for (int i = 1; i < list_FeedbackCards.Count; i++)
        {
            list_FeedbackCards[i].SetActive(false);
        }


        currentCardNum = 0;
        txt_PageNum.text = $"{currentCardNum + 1} / {list_FeedbackCards.Count}";

        

        Obj_Wait.SetActive(false);
        obj_Area_BTns.SetActive(true);

        RectTransform rect_title = Obj_Title.transform.GetComponent<RectTransform>();

        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);
        //------


        yield return new WaitForSeconds(DG_Time);


    }

    public void CardLeftCurl()
    {
        if(currentCardNum - 1 < 0)
        {
            currentCardNum = list_FeedbackCards.Count -1;
        }
        else
        {
            currentCardNum = (currentCardNum - 1) % list_FeedbackCards.Count;
        }

        for (int i = 0; i < list_FeedbackCards.Count; i++)
        {
                if (i == currentCardNum)
                {
                    list_FeedbackCards[i].SetActive(true);
                }
                else
                {
                    list_FeedbackCards[i].SetActive(false);
                }


        }

        MasterAudio.PlaySound("Button_Press");

        txt_PageNum.text = $"{currentCardNum + 1} / {list_FeedbackCards.Count}";
    }

    public void CardRightCurl()
    {

        currentCardNum = (currentCardNum + 1) % list_FeedbackCards.Count;

        for (int i = 0; i < list_FeedbackCards.Count; i++)
        {
            if (i == currentCardNum)
            {
                list_FeedbackCards[i].SetActive(true);
            }
            else
            {
                list_FeedbackCards[i].SetActive(false);
            }


        }
        MasterAudio.PlaySound("Button_Press");
        txt_PageNum.text = $"{currentCardNum + 1} / {list_FeedbackCards.Count}";
    }

    void MakeAnswerCard(FeedBackCardForm userAnswerForm)
    {
        string answer = userAnswerForm.display_Answer;
        SubmitForm userAnswer = userAnswerForm.submitForm;
        int index = userAnswerForm.index;

        print($"{name} : index :  {index} / Question : {userAnswer.txt_Question} / UserAnser : {userAnswer.txt_userAnswer}");

        GameObject np = null;

        // 비디오 URL이 있는지 확인
        if (userAnswer.video_Name != "")
        {
            // 비디오가 포함된 카드 생성
            np = Instantiate(pf_FeedbackVideoCard, obj_Area_Cards.transform);
            np.name = $"pf_FeedBackVideoCard_{index}";
            FeedBackCard feedBackCard = np.GetComponent<FeedBackCard>();

            feedBackCard.Setup(userAnswer.txt_Question, userAnswer.txt_userAnswer, answer, userAnswer.video_Name);
        }
        else
        {
            // 일반 카드 생성
            np = Instantiate(pf_FeedbackCard, obj_Area_Cards.transform);
            np.name = $"pf_FeedBackCard_{index}";
            FeedBackCard feedBackCard = np.GetComponent<FeedBackCard>();

            feedBackCard.Setup(userAnswer.txt_Question, userAnswer.txt_userAnswer, answer);
        }

        list_FeedbackCards.Add(np);
    }

    List<string> GetQList(string rawText)
    {
        List<string> _qSentences = new List<string>();

        // 줄 단위 분리
        string[] lines = rawText.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                _qSentences.Add(trimmed);
            }
        }

        return _qSentences;
    }

    IEnumerator AllUIOff()
    {


        RectTransform rect_title = Obj_Title.transform.GetComponent<RectTransform>();

        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        RectTransform rectContent = Obj_Content.GetComponent<RectTransform>();

        yield return rectContent.DOAnchorPos(new Vector2(0f, -900f), DG_Time).SetEase(DG_Ease).WaitForCompletion();

        yield return new WaitForSeconds(DG_Time);

        Obj_CanvasChoice.SetActive(false);

    }
}
