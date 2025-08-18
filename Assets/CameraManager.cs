using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameObject_N_String
{
    public GameObject gameObj = null;
    public string str = "";
}

public class CameraManager : MonoBehaviour
{
    public static CameraManager inst;

    private void Awake()
    {
        inst = this;
    }

    public List<GameObject_N_String> list_Camera = new List<GameObject_N_String>();

    public List<GameObject_N_String> list_Objs = new List<GameObject_N_String>();

    public void SetCamera(string cameraName)
    {
        // 만약 리스트에 카메라 이름이 없다면 넘어가기
        if (list_Camera.Find(item => item.str == cameraName) == null)
        {
            Debug.Log($"Camera '{cameraName}' not found in the list.");
            return;
        }


        foreach (var item in list_Camera)
        {
            if (item.str == cameraName)
            {
                item.gameObj.SetActive(true);
            }
            else
            {
                item.gameObj.SetActive(false);
            }
        }
    }

    public void SetActivieObj_Off(string objName)
    {
        // 만약 리스트에 카메라 이름이 없다면 넘어가기
        if (list_Objs.Find(item => item.str == objName) == null)
        {
            Debug.Log($"Camera '{objName}' not found in the list.");
            return;
        }

        foreach (var item in list_Objs)
        {
            if (item.str == objName)
            {
                item.gameObj.SetActive(false);
                return;
            }
        }
    }

    public void SetActivieObj_On(string objName)
    {
        // 만약 리스트에 카메라 이름이 없다면 넘어가기
        if (list_Objs.Find(item => item.str == objName) == null)
        {
            Debug.Log($"Camera '{objName}' not found in the list.");
            return;
        }

        foreach (var item in list_Objs)
        {
            if (item.str == objName)
            {
                item.gameObj.SetActive(true);
                return;
            }
        }
    }

    public GameObject GetGameObject(string objName)
    {
        // 만약 리스트에 카메라 이름이 없다면 넘어가기
        if (list_Objs.Find(item => item.str == objName) == null)
        {
            Debug.Log($"Camera '{objName}' not found in the list.");
            return null;
        }

        foreach (var item in list_Objs)
        {
            if (item.str == objName)
            {
                return item.gameObj;
            }
        }

        return null;
    }

    public void SetAnimation(string objName, string aniName)
    {
        GameObject obj = GetGameObject(objName);

        obj.GetComponent<Animator>().SetTrigger(aniName);
    }
}
