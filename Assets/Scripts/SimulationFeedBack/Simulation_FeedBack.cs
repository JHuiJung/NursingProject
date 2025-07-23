using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;

public class Simulation_FeedBack : SimulationBase
{
    public NursingChatClient NursingChatClient; 

    [Header("Canvas Obj & Stuff"), Space(10),SerializeField]
    GameObject Obj_CanvasChoice;

    [SerializeField]
    TMP_Text tmp_Content;

    bool isSimulationEnd = false;
    ScenarioManager _sm;
    public override void Enter(ScenarioManager SM)
    {
        _sm = SM;

        // ȭ�� Ű��
        Obj_CanvasChoice.SetActive(true);

        // ȭ�鿡 ������ ������ �ؽ�Ʈ ���
        PrintUserAnswers();
    }

    public override void Excute(ScenarioManager SM)
    {
    }

    public override void Exit(ScenarioManager SM)
    {

        // ȭ�� ����
        Obj_CanvasChoice.SetActive(false);
        ResetSimulation();

        //%%%%%%%%%%%%%%%%%%%% �ӽ÷� �ǵ�� ����� ���� ���� %%%%%%%%%%%%%%%%%%%%%%
        _sm.str_Answers.Clear();
    }

    public override void ResetSimulation()
    {
        isSimulationEnd = false;
    }

    void PrintUserAnswers()
    {
        string _result = "";

        Stack<string> userStack = new Stack<string>(_sm.str_Answers.ToArray());

        while(userStack.Count > 0)
        {
            string back = userStack.Pop();

            _result += "\n";
            _result += back;
            _result += "\n--------------------------------";
        }

        tmp_Content.text = _result;

        StartCoroutine(NursingChatClient.SendQuestionToAPIUsing(_result));


    }
}
