using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CPUTester
{
    public struct ObstacleData
    {
        public Vector2 a;
        public Vector2 b;
        public Vector2 c;
        public Vector2 d;

        public static explicit operator ObstacleData(Obstacle obstacle)
        {
            return new ObstacleData()
            {
                a = obstacle.vertex[0],
                b = obstacle.vertex[1],
                c = obstacle.vertex[2],
                d = obstacle.vertex[3],
            };
        }

    }

    public static bool isCalculating;

    public static int numAgents;
    public static int numMovements;
    public static int numObstacles;

    public static Line[] agentsPathLines;
    public static Vector2[] collisionPoints;

    public static ObstacleData[] obstaclesArray;

    public static int[] hasAgentCrashed;
    public static int[] indexOfFirstCollision;

    public static Vector2[] lastAgentValidPosition;


    public static void Initialize(int population, int movements, int obstacles, Obstacle[] mapObstacles)
    {
        isCalculating = true;
        numAgents = population;
        numMovements = movements;
        numObstacles = obstacles;

        agentsPathLines = new Line[population * movements];
        mapObstacles.ToList().ForEach(obstacle => obstacle.CalculateVertex());
        obstaclesArray = new ObstacleData[obstacles];

        hasAgentCrashed = new int[population];
        collisionPoints = new Vector2[population];
        indexOfFirstCollision = new int[population];
        lastAgentValidPosition = new Vector2[population];

        for (int i = 0; i < population; i++)
        {
            Dna currentAgentDna = Controller.Instance.population.AgentList[i].Dna;
            for (int j = 0; j < movements - 1; j++)
            {
                agentsPathLines[i * movements + j] = new Line(
                    currentAgentDna.Lines[j],
                    currentAgentDna.Lines[j + 1]);
            }

            hasAgentCrashed[i] = 0;
            collisionPoints[i] = Vector2.zero;
            indexOfFirstCollision[i] = movements - 1;
            lastAgentValidPosition[i] = currentAgentDna.Lines[movements - 1];
        }

        for (int i = 0; i < obstacles; i++)
        {
            obstaclesArray[i] = (ObstacleData)mapObstacles[i];
        }


        for (int i = 0; i < numAgents; i++)
        {
            for (int j = 0; j < numMovements; j++)
            {
                for (int k = 0; k < numObstacles; k++)
                {
                    Calculate(new Vector3Int(i, j, k));
                }
            }
        }


        //Debug.Log("ready");

        Controller.Instance.collisionsCPU = lastAgentValidPosition;
        //isCalculating = false;
    }

    private static bool LineLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 collision)
    {

        // calculate the distance to intersection point
        float uA = ((b2.x - b1.x) * (a1.y - b1.y) - (b2.y - b1.y) * (a1.x - b1.x)) / ((b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y));
        float uB = ((a2.x - a1.x) * (a1.y - b1.y) - (a2.y - a1.y) * (a1.x - b1.x)) / ((b2.y - b1.y) * (a2.x - a1.x) - (b2.x - b1.x) * (a2.y - a1.y));

        // if uA and uB are between 0-1, lines are colliding
        bool hit = uA >= 0 && uA <= 1 && uB >= 0 && uB <= 1;
        if (hit)
            collision = new Vector2(a1.x + (uA * (a2.x - a1.x)), a1.y + (uA * (a2.y - a1.y)));
        else
            collision = Vector2.zero;
        return hit;
    }

    public static bool LineRect(Vector2 u, Vector2 v, Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 collision)
    {
        bool left = LineLine(u, v, a, b, out collision);
        if (left)
            return true;
        bool right = LineLine(u, v, b, c, out collision);
        if (right)
            return true;
        bool top = LineLine(u, v, c, d, out collision);
        if (top)
            return true;
        bool bottom = LineLine(u, v, d, a, out collision);
        if (bottom)
            return true;

        return false;
    }

    // returns true if the line intersects the rect, and populates the collision point. 
    // http://www.jeffreythompson.org/collision-detection/line-rect.php
    public static bool LineRect(Line l, ObstacleData o, out Vector2 collision)
    {
        bool left = LineLine(l.u, l.v, o.a, o.b, out collision);
        if (left)
            return true;
        bool right = LineLine(l.u, l.v, o.b, o.c, out collision);
        if (right)
            return true;
        bool top = LineLine(l.u, l.v, o.c, o.d, out collision);
        if (top)
            return true;
        bool bottom = LineLine(l.u, l.v, o.d, o.a, out collision);
        if (bottom)
            return true;

        return false;
    }

    // returns true if the line intersects the rect, and populates the collision point. 
    // http://www.jeffreythompson.org/collision-detection/line-rect.php
    public static bool LineRectCloser(Line l, ObstacleGPU o, out Vector2 collision)
    {
        Vector2[] collisions = new Vector2[4];
        bool left = LineLine(l.u, l.v, o.a, o.b, out collisions[0]);
        bool right = LineLine(l.u, l.v, o.b, o.c, out collisions[1]);
        bool top = LineLine(l.u, l.v, o.c, o.d, out collisions[2]);
        bool bottom = LineLine(l.u, l.v, o.d, o.a, out collisions[3]);

        int index = 0;
        float minDist = float.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            float dist = Vector2.Distance(l.u, collisions[i]);
            if (minDist > dist)
            {
                minDist = dist;
                index = i;

            }
        }
        collision = collisions[index];

        return left || right || top || bottom;
    }


    public static void Calculate(Vector3Int id)
    {
        /*if (LineRect(LineA, LineB, obstacle.a, obstacle.b, obstacle.c, obstacle.d))
        {
            Debug.Log("CPU: 1");
        }*/

        if ((int)id.x < numAgents && (int)id.y < numMovements && (int)id.z < numObstacles * 4)
        {
            int currentAgentLineIndex = (int)id.x * numMovements + (int)id.y;
            bool intersects = LineRectCloser(
            agentsPathLines[currentAgentLineIndex],
            obstaclesArray[id.z],
            out collisionPoints[id.x]);
            //bool intersects = _lineRect(lineA[id.x], lineB[id.x], obstacle[id.x].a, obstacle[id.x].b, obstacle[id.x].c, obstacle[id.x].d, collision);

            bool hasIntersected = ((hasAgentCrashed[id.x] == 1) || intersects) ? true : false;

            hasAgentCrashed[id.x] = hasIntersected ? 1 : 0;

            bool improves = (int)id.y < indexOfFirstCollision[id.x];

            indexOfFirstCollision[id.x] = intersects && improves ? id.y : indexOfFirstCollision[id.x];

            lastAgentValidPosition[id.x] = hasAgentCrashed[id.x] == 1 && improves ? collisionPoints[id.x] : lastAgentValidPosition[id.x];

        }
    }
}
