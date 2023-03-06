using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CPUTester
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

    public bool isCalculating;

    public int numAgents;
    public int numMovements;
    public int numObstacles;
    private TypeOfDistance typeOfDistance;
    private Vector2 target;

    public Line[] agentsPathLines;
    public Vector2[] collisionPoints;

    public ObstacleData[] obstaclesArray;

    public int[] hasAgentCrashed;
    public int[] indexOfFirstCollision;

    public Vector2[] lastAgentValidPosition;

    public Vector2[] bestPosition;
    public float[] bestPositionDistance;

    public EliteDna[] populationDna;

    public void Initialize(int population, int iterations, int movements, int obstacles, Obstacle[] mapObstacles, TypeOfDistance typeOfDistance, Vector2 target)
    {
        isCalculating = true;
        numAgents = population;
        numMovements = movements;
        numObstacles = obstacles;
        this.typeOfDistance = typeOfDistance;
        this.target = target;

        populationDna = new EliteDna[population];

        agentsPathLines = new Line[population * movements];
        mapObstacles.ToList().ForEach(obstacle => obstacle.CalculateVertex());
        obstaclesArray = new ObstacleData[obstacles];

        hasAgentCrashed = new int[population];
        collisionPoints = new Vector2[population];
        indexOfFirstCollision = new int[population];
        lastAgentValidPosition = new Vector2[population];
        bestPosition = new Vector2[population];
        bestPositionDistance = new float[population];

        for (int i = 0; i < population; i++)
        {
            Dna currentAgentDna = new Dna();
            populationDna[i] = new EliteDna(currentAgentDna);
            for (int j = 0; j < movements - 1; j++)
            {
                agentsPathLines[i * movements + j] = new Line(
                    currentAgentDna.Lines[j],
                    currentAgentDna.Lines[j + 1]);
            }

            hasAgentCrashed[i] = 0;
            collisionPoints[i] = Vector2.zero;
            indexOfFirstCollision[i] = movements - 1;
            bestPosition[i] = currentAgentDna.Lines[movements - 1];
            bestPositionDistance[i] = float.MaxValue;
        }

        for (int i = 0; i < obstacles; i++)
        {
            obstaclesArray[i] = (ObstacleData)mapObstacles[i];
        }

        for (int i = 0; i < iterations; i++)
        {
            CPUIteration();
        }
    }


    private bool LineLine(Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2, out Vector2 collision)
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

    public bool LineRect(Vector2 u, Vector2 v, Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 collision)
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
    public bool LineRect(Line l, ObstacleData o, out Vector2 collision)
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
    public bool LineRectCloser(Line l, ObstacleData o, out Vector2 collision)
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


    public void Calculate(Vector3Int id)
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

            bool improves = (int)id.y < indexOfFirstCollision[id.x];

            float distanceToTargetThisMovement = intersects ? CalculateDistance(collisionPoints[id.x]) : CalculateDistance(agentsPathLines[currentAgentLineIndex].v);

            bool improvesBestDistance = bestPositionDistance[id.x] < distanceToTargetThisMovement;

            bestPositionDistance[id.x] = (hasAgentCrashed[id.x] != 1 && improvesBestDistance) ? distanceToTargetThisMovement : bestPositionDistance[id.x];

            hasAgentCrashed[id.x] = hasIntersected ? 1 : 0;

            indexOfFirstCollision[id.x] = intersects && improves ? id.y : indexOfFirstCollision[id.x];

            lastAgentValidPosition[id.x] = hasAgentCrashed[id.x] == 1 && improves ? collisionPoints[id.x] : lastAgentValidPosition[id.x];

        }


    }
    void CPUIteration()
    {
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
        //calculateFitness
        CalculatePopulationFitness();
        //add iteration data base
        //learning period
        //selection
        //reproduction
        //setelites

        Controller.Instance.collisionsCPU = lastAgentValidPosition;
    }

    private void CalculatePopulationFitness()
    {
        float fitness = 0;
        for (int i = 0; i < numAgents; i++)
        {
            float distanceToTarget = CalculateDistance(lastAgentValidPosition[i]);
            if (distanceToTarget < 1)
            {
                distanceToTarget = 1;
            }

            float bestDistance = CalculateDistance(lastAgentValidPosition[i]);
            if (bestDistance < 1)
            {
                bestDistance = 1;
            }

            fitness = 1000 * 1 / distanceToTarget * 1 / bestDistance;

            if (hasAgentCrashed[i] == 1)
                fitness *= .5f;
            if (reachedTarget)
            {
                fitness *= 4;
                fitness *= (((dna.Genes.Count - lastStep) / (float)dna.Genes.Count) + 1);
            }
        }
    }
    private float CalculateDistance(Vector2 finalPoint)
    {
        switch (Controller.Settings.typeOfDistance)
        {
            case TypeOfDistance.Manhattan:
                return Mathf.Abs(finalPoint.x - target.x) + Mathf.Abs(finalPoint.y - target.y);
            case TypeOfDistance.Chebyshev:
                return Mathf.Max(finalPoint.x - target.x, finalPoint.y - target.y);
            case TypeOfDistance.Euclidean:
            default:
                return Mathf.Sqrt(Mathf.Pow(finalPoint.x - target.x, 2) + Mathf.Pow(finalPoint.y - target.y, 2));
        }
    }
}
