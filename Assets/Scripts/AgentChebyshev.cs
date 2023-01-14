using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentChebyshev : Agent
{
    protected override float CalculateDistance()
    {
        return Mathf.Max(transform.position.x - target.x, transform.position.y - target.y);
    }
}
