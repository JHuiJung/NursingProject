using DarkTonic.MasterAudio;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Video;

public class Simulation_Video : SimulationBase
{
    [Header("Canvas Obj & Stuff"), SerializeField]
    GameObject Obj_CanvasChoice;
    [SerializeField]
    GameObject Obj_Area_Video;
    [SerializeField]
    GameObject Obj_BTN_Next;

    [Header("Video")]
    public VideoPlayer videoPlayer;
    public string video_Name = "SampleVideo"; //비디오 이름(StreamingAssets 폴더에 있는 mp4 파일 이름)

    [TextArea] //질문
    [Header("질문(필수로 입력)")]
    public string text_Question = "";
    public TMP_Text Tmp_Question;

    [Header("Dotween"),]
    public float DG_Time = 0.75f;
    public float DG_Area_EndY = -20f;
    public float DG_Area_StartY = -450f;
    public Ease DG_Ease = Ease.InOutQuad;


    

    string videoPath = "";

    // 시뮬레이션 끝 bool
    bool isSimulationEnd = false;

    ScenarioManager _sm;

    public override void Enter(ScenarioManager SM)
    {
        print($"{name} : 문제 시작");

        Obj_CanvasChoice.SetActive(true);
        _sm = SM;

        Setup_Normal();

        StartCoroutine(AllUiOn());
    }

    public override void Excute(ScenarioManager SM)
    {
        if (isSimulationEnd) return;

        if (videoPlayer != null && videoPlayer.isPrepared && !videoPlayer.isPlaying)
        {
            if (videoPlayer.time >= videoPlayer.length - 0.1f)
            {
                Obj_BTN_Next.SetActive(true);
            }
        }

    }

    public override void Exit(ScenarioManager SM)
    {
        print($"{name} : 문제 끝");
        ResetSimulation();
        Obj_CanvasChoice.SetActive(false);
    }
    public override void ResetSimulation()
    {
        isSimulationEnd = false;

        Obj_BTN_Next.SetActive(false);
    }

    void Setup_Normal()
    {
        // 질문 텍스트 수정
        Tmp_Question.text = text_Question;

        if (!string.IsNullOrEmpty(video_Name))
        {
            videoPath = Application.streamingAssetsPath + "/" + video_Name + ".mp4";
        }

        if (videoPlayer != null)
        {
            // 영상이 끝났을 때 호출될 이벤트 등록
            videoPlayer.loopPointReached += OnVideoEnd;
        }

        // Next 버튼은 처음엔 꺼두기
        Obj_BTN_Next.SetActive(false);
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        // 영상 끝나면 Next 버튼 켜기
        Obj_BTN_Next.SetActive(true);
    }

    public void PlayVideo()
    {
        if (videoPlayer != null && videoPath != "")
        {
            MasterAudio.PlaySound("Button_Press");
            videoPlayer.url = videoPath;
            videoPlayer.SetDirectAudioVolume(0, 0f); // Mute audio
            videoPlayer.Play();
        }
        else
        {
            Debug.Log("비디오 실행 오류");
        }
    }

    public void StopVideo()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            MasterAudio.PlaySound("Button_Press");
            videoPlayer.Stop();
        }
    }

    public void SubmitAnswer()
    {
        if (isSimulationEnd) return;

        Obj_BTN_Next.SetActive(false);
        MasterAudio.PlaySound("Button_Press");

        StartCoroutine(AllUiOff());
    }

    IEnumerator AllUiOn()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            0f), DG_Time).SetEase(DG_Ease);

        // 텍스트 입력 DG
        RectTransform rect_AreaTI = Obj_Area_Video.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_EndY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);
    }

    IEnumerator AllUiOff()
    {
        // 타이틀 DG
        RectTransform rect_title = Tmp_Question.gameObject.transform.parent
            .GetComponent<RectTransform>();


        rect_title.DOAnchorPos(new Vector2(rect_title.anchoredPosition.x,
            200f), DG_Time).SetEase(DG_Ease);

        // 텍스트 입력 DG
        RectTransform rect_AreaTI = Obj_Area_Video.GetComponent<RectTransform>();

        rect_AreaTI.DOAnchorPos(new Vector2(rect_AreaTI.anchoredPosition.x, DG_Area_StartY), DG_Time
            ).SetEase(DG_Ease);

        yield return new WaitForSeconds(DG_Time);

        // 다음 시뮬레이션으로 이동
        _sm.NextSimulation();
    }
}
