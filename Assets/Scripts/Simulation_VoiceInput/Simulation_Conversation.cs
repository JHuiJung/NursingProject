using DarkTonic.MasterAudio;
using DG.Tweening;
using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using uMicrophoneWebGL;
using UnityEngine;
using UnityEngine.Networking;

public class Simulation_Conversation : SimulationBase
{
    [Header("Canvas Obj & Stuff"), Space(10), SerializeField]
    GameObject Obj_CanvasChoice;
    public GameObject Obj_Area_Wait;
    public GameObject Obj_Btn_Next;
    //카메라 메니저에 접근하기 위한 정보들
    public string opposite_Cam_Name = "ParentSide";
    public string opposite_Obj_Name = "Parent";
    public string player_Cam_Name = "PlayerSide";
    public string player_Obj_Name = "Player";

    [TextArea]
    [Header("질문 (반드시 포함할 것)"), Space(10)]
    public string text_Question = "";
    public TMP_Text Tmp_Question;
    [TextArea]
    public string keywords = "";

    [Header("Voice Input"), Space(10)]
    public TMP_Text txt_VoiceUserInput;
    public GameObject Obj_Area_VoiceInput;
    public GameObject Obj_Btn_StartRecord;
    public GameObject Obj_Btn_StopRecord;
    public GameObject Obj_BTN_Submit;

    [Header("ConvBox"), Space(10)]
    public int minConvCount = 2;
    public GameObject Obj_Area_ConvBox;
    public GameObject pf_User_ConvBox;
    public GameObject pf_Opposite_ConvBox;
    public string opposite_Name = "보호자";
    public string opposite_Content = "";
    public int currentConvCount = 0;

    [Header("Dotween"), Space(10)]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -20f;
    public float DG_Area_StartY = -450f;
    public Ease DG_Ease = Ease.Linear;

    // �ùķ��̼� �� bool
    bool isSimulationEnd = false;
    ScenarioManager _sm;

    //----ai �亯----
    string aiParentResponse = "";

    public override void Enter(ScenarioManager SM)
    {
        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup();
        StartCoroutine(AllUiOn());
        StartCoroutine(Start_Simulation());
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

        CheckSTT_Text();
        CheckConvCnt();


    }

    public void CheckConvCnt()
    {
        if(currentConvCount >= minConvCount && !isSimulationEnd)
        {
            Obj_Btn_Next.SetActive(true);
        }
    }

    public override void Exit(ScenarioManager SM)
    {
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;

        txt_VoiceUserInput.text = "";
        aiParentResponse = "";
        currentConvCount = 0;

        Obj_Btn_StartRecord.SetActive(false);
        Obj_Btn_StopRecord.SetActive(false);
        Obj_Area_Wait.SetActive(false);
        Obj_Btn_Next.SetActive(false);

        for (int i = Obj_Area_ConvBox.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(Obj_Area_ConvBox.transform.GetChild(i).gameObject);
        }

    }

    void CheckSTT_Text()
    {
        string sttText = STT_TTS_Manager.inst.stt_Text;

        if(sttText == "") return;

        // set userbox text
        //Debug.Log($"?? STT 응답: {sttText} / 개수 {sttText.Length}");
        txt_VoiceUserInput.text = sttText;
        Obj_BTN_Submit.SetActive(true);
    }

    void Setup()
    {
        currentConvCount = 0;
        Tmp_Question.text = text_Question;
    }
    //----- �ùķ��̼� ���� ------

    IEnumerator Start_Simulation()
    {
        
        yield return StartCoroutine(ParentReadQuestion());

        yield return StartCoroutine(MakeUserConvBox());

    }

    IEnumerator ParentReadQuestion()
    {
        // Opposite ConvBox ����
        GameObject oppositeConvbox = Instantiate(pf_Opposite_ConvBox, Obj_Area_ConvBox.transform);
        oppositeConvbox.transform.SetAsLastSibling();
        oppositeConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        oppositeConvbox.GetComponent<ConvBox>().Setup(opposite_Name, opposite_Content);

        // Opposite Cam On
        CameraManager.inst.SetCamera(opposite_Cam_Name);

        // Opposite Ani Talk On
        CameraManager.inst.SetAnimation(opposite_Obj_Name, "talk");

        DataManager.inst.SetMute(true);

        // TTS�� ����
        yield return StartCoroutine(STT_TTS_Manager.inst.TTS(opposite_Content));

        DataManager.inst.SetMute(false);

        // Opposite Ani idle On
        CameraManager.inst.SetAnimation(opposite_Obj_Name, "idle");
    }

    IEnumerator ParentResponse()
    {
        // ai ���� �亯 �ޱ�
        Obj_Btn_StartRecord.SetActive(false);
        Obj_Btn_StopRecord.SetActive(false);

        Obj_Area_Wait.SetActive(true);

        yield return StartCoroutine(GetParentResponse());
        string ai_responese = "";
        if (!string.IsNullOrEmpty(aiParentResponse))
        {
            ai_responese = aiParentResponse;
        }
        else
        {
            ai_responese = "ai응답이 없습니다";
        }

        Obj_Area_Wait.SetActive(false);

        // convBox ��ĭ �ø���
        yield return StartCoroutine(AllConvBoxMoveUp());

        // Opposite ConvBox ����
        GameObject oppositeConvbox = Instantiate(pf_Opposite_ConvBox, Obj_Area_ConvBox.transform);
        oppositeConvbox.transform.SetAsLastSibling();
        oppositeConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        oppositeConvbox.GetComponent<ConvBox>().Setup(opposite_Name, ai_responese);

        // Opposite Cam On
        CameraManager.inst.SetCamera(opposite_Cam_Name);

        // Opposite Ani Talk On
        CameraManager.inst.SetAnimation(opposite_Obj_Name, "talk");

        DataManager.inst.SetMute(true);

        //tts�� ���
        //yield return StartCoroutine(PlayTTSQuestion(ai_responese));
        yield return StartCoroutine(STT_TTS_Manager.inst.TTS(ai_responese));

        DataManager.inst.SetMute(false);

        // Opposite Ani idle On
        CameraManager.inst.SetAnimation(opposite_Obj_Name, "idle");
    }

    IEnumerator MakeUserConvBox()
    {
        // convBox ��ĭ �ø���
        yield return StartCoroutine(AllConvBoxMoveUp());

        // BTN Active
        Obj_Btn_StartRecord.SetActive(true);
        Obj_Btn_StopRecord.SetActive(false);

        // Player Cam On
        CameraManager.inst.SetCamera(player_Cam_Name);

        // UserConvBox ����
        GameObject userConvbox = Instantiate(pf_User_ConvBox, Obj_Area_ConvBox.transform);
        userConvbox.transform.SetAsLastSibling();
        userConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        userConvbox.GetComponent<ConvBox>().Setup($"{DataManager.inst.userName} 간호사", "");
        txt_VoiceUserInput = userConvbox.GetComponent<ConvBox>().txt_Content;
    }

    IEnumerator NextConv()
    {
        yield return StartCoroutine(ParentResponse());

        yield return StartCoroutine(MakeUserConvBox());
    }

    //IEnumerator End_Simulation()
    //{
    //    // ai ���� �亯 �ޱ�
    //    Obj_Btn_StartRecord.SetActive(false);
    //    Obj_Btn_StopRecord.SetActive(false);

    //    Obj_Area_Wait.SetActive(true);

    //    yield return StartCoroutine(GetParentResponse());
    //    string ai_responese = "";
    //    if (!string.IsNullOrEmpty(aiParentResponse))
    //    {
    //        ai_responese = aiParentResponse;
    //    }
    //    else
    //    {
    //        ai_responese = "ai응답이 없습니다";
    //    }

    //    Obj_Area_Wait.SetActive(false);

    //    // convBox ��ĭ �ø���
    //    yield return StartCoroutine(AllConvBoxMoveUp());

    //    // Opposite ConvBox ����
    //    GameObject oppositeConvbox = Instantiate(pf_Opposite_ConvBox, Obj_Area_ConvBox.transform);
    //    oppositeConvbox.transform.SetAsLastSibling();
    //    oppositeConvbox.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    //    oppositeConvbox.GetComponent<ConvBox>().Setup(opposite_Name, ai_responese);

    //    // Opposite Cam On
    //    CameraManager.inst.SetCamera(opposite_Cam_Name);

    //    // Opposite Ani Talk On
    //    CameraManager.inst.SetAnimation(opposite_Obj_Name, "talk");

    //    //tts�� ���
    //    //yield return StartCoroutine(PlayTTSQuestion(ai_responese));
    //    yield return StartCoroutine(STT_TTS_Manager.inst.TTS(ai_responese));

    //    // Opposite Ani idle On
    //    CameraManager.inst.SetAnimation(opposite_Obj_Name, "idle");

    //}

    public void Next()
    {
        isSimulationEnd = true;
        StartCoroutine(AllUiOff());
    }

    IEnumerator AllConvBoxMoveUp()
    {
        int cnt = Obj_Area_ConvBox.transform.childCount;
        float timeOffset = 0.5f;

        for (int i = 0; i < Obj_Area_ConvBox.transform.childCount; i++)
        {
            ConvBox convBox = Obj_Area_ConvBox.transform.GetChild(i).GetComponent<ConvBox>();

            convBox.MoveUp(timeOffset);
        }

        yield return new WaitForSeconds(timeOffset * cnt);
    }

    //------------------------------------------------------------------------------------------

    #region ----------------------------------------STT 
    public void ToggleRecord()
    {

        bool isMrocording = STT_TTS_Manager.inst.isMRecording;

        if (!isMrocording)
        {
            // recording - begin
            txt_VoiceUserInput.text = "";

            Obj_Btn_StartRecord.SetActive(false);
            Obj_Btn_StopRecord.SetActive(true);

            // Player Ani Talk On
            DataManager.inst.SetMute(true);
            CameraManager.inst.SetAnimation(player_Obj_Name, "talk");

        }
        else
        {
            // no Recording - end

            Obj_Btn_StartRecord.SetActive(true);
            Obj_Btn_StopRecord.SetActive(false);

            // Player Ani idle On
            DataManager.inst.SetMute(false);
            CameraManager.inst.SetAnimation(player_Obj_Name, "idle");
        }

        MasterAudio.PlaySound("Button_Press");

        STT_TTS_Manager.inst.ToggleRecord();
    }

    #endregion

    //���� �� UI

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        
        Obj_BTN_Submit.SetActive(false);
        Obj_Btn_StartRecord.SetActive(false);
        Obj_Btn_StopRecord.SetActive(false);

        MasterAudio.PlaySound("Button_Press");

        string answer = txt_VoiceUserInput.text;
        STT_TTS_Manager.inst.stt_Text = string.Empty;


        /*
        SubmitForm submitForm = new SubmitForm();
        submitForm.txt_Question = text_Question;
        submitForm.txt_QuestionAnswer = $"상대방 질문 : {opposite_Content} / 유저의 답변에 포함되야할 키워드 : {keywords} " +
            $" / 상대방의 질문과 키워드를 참고해서 정답, 오답 판별";
        submitForm.txt_userAnswer = answer;
        submitForm.useAiAnswer = true;
        submitForm.quiz_index = simulation_Quiz_Index;

        _sm.str_Answers.Add(submitForm);
        */

        currentConvCount++;

        StartCoroutine(NextConv());
    }

    public IEnumerator GetParentResponse()
    {

        string userRes = txt_VoiceUserInput.text;

        WWWForm form = new WWWForm();
        form.AddField("parent_question", opposite_Content);
        form.AddField("user_response", userRes);
        form.AddField("keywords", keywords);

        string url = APIConfig.Instance.ParentResponse;

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var result = JSON.Parse(request.downloadHandler.text);
            string followupText = result["parent_response"];
            aiParentResponse = followupText;
            Debug.Log("? 응답: " + followupText);
        }
        else
        {
            Debug.LogError("? 응답 오류: " + request.error);
        }
    }

    IEnumerator AllUiOn()
    {
        // Ÿ��Ʋ DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // ���̽� �Է� DG
        RectTransform rect_AreaTI = Obj_Area_VoiceInput.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_EndY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);
    }

    IEnumerator AllUiOff()
    {
        Obj_Btn_Next.SetActive(false);

        // Ÿ��Ʋ DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);

        // ���� �ùķ��̼����� �̵�
        _sm.NextSimulation();


    }
}
