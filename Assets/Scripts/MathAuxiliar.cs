using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathAuxiliar
{
    public static float NormalizeValue(float min, float max, float value)
    {
        return 1 - ((value - min) / (max - min));
    }
}
