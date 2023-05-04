using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentChebyshev : Agent
{
    protected override float CalculateDistance()
    {
        return Mathf.Max(Mathf.Abs(transform.position.x - target.x), Mathf.Abs(transform.position.y - target.y));
    }
}
