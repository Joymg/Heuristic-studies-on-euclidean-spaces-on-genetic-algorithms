using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Obstacle : MonoBehaviour
{
    public Vector2[] vertex = new Vector2[4];

    public BoxCollider2D collider;

    [ContextMenu("Calclate Vertex")]
    public void CalculateVertex()
    {
        collider = GetComponent<BoxCollider2D>();

        Vector2[] obstaclePoints = new Vector2[4];
        var size = collider.size * 0.5f;

        var mtx = Matrix4x4.TRS(collider.bounds.center, collider.transform.localRotation, collider.transform.localScale);

        obstaclePoints[0] = mtx.MultiplyPoint3x4(new Vector2(-size.x, size.y));
        obstaclePoints[1] = mtx.MultiplyPoint3x4(new Vector2(-size.x, -size.y));
        obstaclePoints[2] = mtx.MultiplyPoint3x4(new Vector2(size.x, -size.y));
        obstaclePoints[3] = mtx.MultiplyPoint3x4(new Vector2(size.x, size.y));

        vertex = obstaclePoints;
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < vertex.Length; i++)
        {
            Gizmos.DrawSphere(vertex[i], .1f);
            Handles.Label(vertex[i], i.ToString());
            Gizmos.DrawLine(vertex[i], vertex[(i + 1) % vertex.Length]);
        }

    }
}
