using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentEuclidean : Agent
{
    protected override float CalculateDistance()
    {
        return Mathf.Sqrt(Mathf.Pow(transform.position.x - target.x, 2) + Mathf.Pow(transform.position.y - target.y, 2));
    }
}
