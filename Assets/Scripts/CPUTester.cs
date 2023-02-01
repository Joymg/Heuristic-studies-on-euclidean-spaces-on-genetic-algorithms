using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CPUTester
{

    private static bool LineLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
    {
        // calculate the distance to intersection point
        float uA = ((b2.x - b1.x) * (a1.y - b1.y) - (b2.y - b1.y) * (a1.x - b1.x)) / ((b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y));
        float uB = ((a2.x - a1.x) * (a1.y - b1.y) - (a2.y - a1.y) * (a1.x - b1.x)) / ((b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y));

        // if uA and uB are between 0-1, lines are colliding
        bool hit = uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1;
        return hit;
    }

    public static bool LineRect(Vector2 u, Vector2 v, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        bool left = LineLine(u, v, a, b);
        if (left)
            return true;
        bool right = LineLine(u, v, b, c);
        if (right)
            return true;
        bool top = LineLine(u, v, c, d);
        if (top)
            return true;
        bool bottom = LineLine(u, v, d, a);
        if (bottom)
            return true;

        return false;
    }


    public static void Calculate(Vector2 LineA, Vector2 LineB, Controller.Obs obstacle)
    {
        if (LineRect(LineA, LineB, obstacle.x, obstacle.y, obstacle.z, obstacle.w))
        {
            Debug.Log("CPU: 1");
        }
    }
}
