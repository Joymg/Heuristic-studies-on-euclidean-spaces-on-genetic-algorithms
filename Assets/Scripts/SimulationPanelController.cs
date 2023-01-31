using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimulationPanelController : MonoBehaviour
{
    public TextMeshProUGUI typeOfDistanceText;
    public TextMeshProUGUI iterationNumber;
    public TextMeshProUGUI arrivedNumber;
    public TextMeshProUGUI crashedNumber;
    public TextMeshProUGUI ratioNumber;
    public TextMeshProUGUI firstArrivedNumber;

    Population population;
    Controller controller;

    int iterationNum;
    int agentsArrived;
    int agentsCrashed;
    int ratio;
    int firstArrived;
    bool didFirstAgentArrived;

    private void Start()
    {
        population = FindObjectOfType<Population>();
        switch (population.typeOfDistance)
        {
            case TypeOfDistance.Euclidean:
                typeOfDistanceText.text = "Euclidean";
                break;
            case TypeOfDistance.Manhattan:
                typeOfDistanceText.text = "Manhattan";
                break;
            case TypeOfDistance.Chebyshev:
                typeOfDistanceText.text = "Chebyshev";
                break;
        }

        iterationNum = 0;
        agentsArrived = 0;
        agentsCrashed = 0;
        ratio = 0;
        firstArrived = 0;
        didFirstAgentArrived = false;

        Controller.Instance.IncrementIteration += IncrementIterationNumber;
        population.AgentArrivedToTarget += IncrementAgentsArrived;
        Controller.Instance.AgentCrashed += IncrementAgentsCrashed;
        Controller.Instance.IncrementIteration += UpdateRatio;
        population.AgentArrivedToTarget += UpdateFirstArrived;
    }

    private void ResetVariables()
    {
        agentsArrived = 0;
        agentsCrashed = 0;
    }


    public void IncrementIterationNumber()
    {
        iterationNum++;
        iterationNumber.text = iterationNum.ToString();
    }

    public void IncrementAgentsArrived()
    {
        agentsArrived++;
        arrivedNumber.text = agentsArrived.ToString();
    }

    public void IncrementAgentsCrashed()
    {
        agentsCrashed++;
        crashedNumber.text = agentsCrashed.ToString();
    }

    public void UpdateRatio()
    {
        ratio = (int)(((float)agentsArrived / population.NumMovements) * 100);
        ratioNumber.text = ratio.ToString();
        ResetVariables();
    }

    public void UpdateFirstArrived()
    {
        if (!didFirstAgentArrived)
        {
            firstArrivedNumber.text = iterationNum.ToString();
            didFirstAgentArrived = true;
        }
    }
}
