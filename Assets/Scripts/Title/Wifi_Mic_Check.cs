using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.SceneManagement;

public class Wifi_Mic_Check : MonoBehaviour
{
    public GameObject obj_Wifi_On;
    public GameObject obj_Wifi_Off;
    public GameObject obj_Mic_On;
    public GameObject obj_Mic_Off;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void CheckMicPermission();
#endif

    private void Start()
    {
        gameObject.name = "WifiMicChecker"; // JS SendMessage 대상
        CheckWifi();
    }

    void CheckWifi()
    {
        // 인터넷 연결 여부만 확인 (WebGL에서는 세부 네트워크 타입 확인 불가)
        bool isConnected = Application.internetReachability != NetworkReachability.NotReachable;

        obj_Wifi_On.SetActive(isConnected);
        obj_Wifi_Off.SetActive(!isConnected);

        // Wi-Fi 확인 후 마이크 확인
        CheckMic();
    }

    void CheckMic()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        CheckMicPermission(); // JS 호출
#else
        // WebGL 외 환경에서는 Unity Microphone API 사용 가능
        bool micAvailable = Microphone.devices.Length > 0;
        obj_Mic_On.SetActive(micAvailable);
        obj_Mic_Off.SetActive(!micAvailable);
#endif
    }

    // JS에서 마이크 결과 전달
    public void OnMicResult(string result)
    {
        bool micAvailable = result == "true";
        obj_Mic_On.SetActive(micAvailable);
        obj_Mic_Off.SetActive(!micAvailable);
    }
}
