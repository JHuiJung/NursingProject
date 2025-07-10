using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BTN_Choice : MonoBehaviour
{
    public Simulation_Choice simulation_Choice;

    public int BTN_Number = 0;

    public string TXT_Answer = "";
    public TMPro.TMP_Text Txt_TMP;

    public void SetBTN(string _content, int _num, Simulation_Choice SC)
    {
        BTN_Number = _num;
        TXT_Answer = _content;
        Txt_TMP.text = TXT_Answer;
        simulation_Choice = SC;
    }

    public void Submit()
    {
        simulation_Choice.SubmitAnswer(TXT_Answer, BTN_Number);
    }

}
