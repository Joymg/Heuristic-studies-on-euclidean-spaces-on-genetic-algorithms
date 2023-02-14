using UnityEngine;

public struct Line
{
    public Vector2 u, v;

    public Line(Vector2 x, Vector2 y) : this()
    {
        u = x;
        v = y;
    }
}
