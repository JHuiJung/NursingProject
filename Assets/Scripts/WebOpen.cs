using System.Runtime.InteropServices;
using UnityEngine;

public class WebOpen : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void OpenNewTab(string url);

    public string siteURL = "https://example.com";

    public void OpenSite()
    {
        OpenNewTab(siteURL);
    }
}
