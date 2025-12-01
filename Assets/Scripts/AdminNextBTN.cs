using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdminNextBTN : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if(DataManager.inst.userName=="admin")
        {
            this.gameObject.SetActive(true);
        }
        else
        {
            this.gameObject.SetActive(false);
        }
    }
}
