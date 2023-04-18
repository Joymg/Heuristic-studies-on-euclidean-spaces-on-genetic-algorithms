using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class CPUTester
{
    [System.Serializable]
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
    [SerializeField] private int currentSimulationIndex;
    public bool isCalculating;

    public int numAgents;
    public int numMovements;
    public int numObstacles;
    private TypeOfDistance typeOfDistance;
    private Vector2 spawn;
    private Vector2 target;

    public Line[] agentsPathLines;
    public Vector2[] collisionPoints;

    public ObstacleData[] obstaclesArray;

    public int[] hasAgentCrashed;
    public int[] indexOfFirstCollision;

    public Vector2[] lastAgentValidPosition;
    public int[] indexOfLastAgentValidMovement;

    public int[] hasAgentReachedTarget;

    public Vector2[] bestPosition;
    public float[] bestPositionDistance;

    public float[] populationFitness;

    public EliteDna[] populationDna;
    private List<EliteDna> currentElite;
    private List<EliteDna> learningPeriodAccumulatedElite;

    private List<EliteDna> matingPool;

    public int SuccessfulAgents => hasAgentReachedTarget.ToList().Count(num => num == 1);
    public int CrashedAgents => hasAgentCrashed.ToList().Count(num => num == 1);

    public float RatioOfSuccess => (float)SuccessfulAgents / numAgents;
    public float AverageFitness => populationDna.Sum(agent => agent.fitness) / numAgents;
    public float MaxFitness => populationDna.Max(agent => agent.fitness);
    public float MinFitness => populationDna.Min(agent => agent.fitness);
    public float MedianFitness
    {
        get
        {
            populationDna.OrderBy(x => x.fitness);
            return numAgents % 2 == 0 ? (populationDna[numAgents / 2].fitness + populationDna[(int)(numAgents / 2) - 1].fitness) / 2 : populationDna[numAgents / 2].fitness;
        }
    }

    public float StandardDeviationFitness
    {
        get
        {
            populationDna.OrderBy(x => x.fitness);
            float sum = 0;
            for (int i = 0; i < numAgents; i++)
            {
                sum += populationDna[i].fitness * populationDna[i].fitness;
            }
            var avg = AverageFitness;
            return Mathf.Sqrt(((sum * sum) / numAgents) - (avg * avg));
        }
    }

    public float VarianceFitness
    {
        get
        {
            var standardDeviation = StandardDeviationFitness;
            return standardDeviation * standardDeviation;
        }
    }


    public void Initialize(int population, int simulations, int iterations, int movements, int obstacles, Obstacle[] mapObstacles, TypeOfDistance typeOfDistance, Vector2 target, Vector2 spawn)
    {
        //GenerateNewPopulation(population, movements, obstacles, mapObstacles, typeOfDistance, target, spawn);
        isCalculating = true;
        numAgents = population;
        numMovements = movements;
        numObstacles = obstacles;
        this.typeOfDistance = typeOfDistance;
        this.spawn = spawn;
        this.target = target;

        populationDna = new EliteDna[population];
        currentElite = new List<EliteDna>();
        learningPeriodAccumulatedElite = new List<EliteDna>();
        matingPool = new List<EliteDna>();


        agentsPathLines = new Line[population * movements];
        mapObstacles.ToList().ForEach(obstacle => obstacle.CalculateVertex());
        obstaclesArray = new ObstacleData[obstacles];

        hasAgentCrashed = new int[population];
        hasAgentReachedTarget = new int[population];
        collisionPoints = new Vector2[population];
        indexOfFirstCollision = new int[population];
        lastAgentValidPosition = new Vector2[population];
        indexOfLastAgentValidMovement = new int[population];
        bestPosition = new Vector2[population];
        bestPositionDistance = new float[population];

        currentSimulationIndex = 1;

        for (int i = 0; i < obstacles; i++)
        {
            obstaclesArray[i] = (ObstacleData)mapObstacles[i];
        }

        //for (int i = 0; i < numAgents; i++)
        //{
        //    for (int j = 0; j < numMovements; j++)
        //    {
        //        for (int k = 0; k < numObstacles; k++)
        //        {
        //            Calculate(new Vector3Int(i, j, k));
        //        }
        //    }
        //}
        for (int i = 0; i < simulations; i++)
        {

            GenerateNewPopulation(population, movements, spawn);
            Database.AddSimulation(new Database.Database_SimulationEntry(typeOfDistance, population, movements, Controller.Settings.elitism, Controller.Settings.mutationProb, Controller.Settings.map));
            currentSimulationIndex = Database.GetNumSimulationsInDatabse();
            for (int j = 0; j < iterations; j++)
            {
                CPUIteration();
            }
        }
    }

    private void GenerateNewPopulation(int population, int movements, Vector3 spawn)
    {

        for (int i = 0; i < population; i++)
        {
            //Dna currentAgentDna = Controller.Instance.population.AgentList[i].Dna;

            Dna currentAgentDna = new Dna();
            populationDna[i] = new EliteDna(currentAgentDna);

            agentsPathLines[i * movements] = new Line(
                    spawn,
                    currentAgentDna.Lines[0]);
            for (int j = 1; j < movements; j++)
            {
                agentsPathLines[i * movements + j] = new Line(
                    currentAgentDna.Lines[j - 1],
                    currentAgentDna.Lines[j]);
            }

            hasAgentCrashed[i] = 0;
            hasAgentReachedTarget[i] = 0;
            collisionPoints[i] = Vector2.zero;
            indexOfFirstCollision[i] = movements - 1;
            indexOfLastAgentValidMovement[i] = movements - 1;
            bestPosition[i] = currentAgentDna.Lines[movements - 1];
            bestPositionDistance[i] = float.MaxValue;
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

            bool hasIntersected = ((hasAgentCrashed[id.x] == 1) || intersects) ? true : false;

            bool improves = (int)id.y < indexOfFirstCollision[id.x];

            float distanceToTargetThisMovement = intersects ? CalculateDistance(collisionPoints[id.x]) : CalculateDistance(agentsPathLines[currentAgentLineIndex].v);

            bool improvesBestDistance = bestPositionDistance[id.x] > distanceToTargetThisMovement;

            hasAgentReachedTarget[id.x] = (hasAgentReachedTarget[id.x] == 1) || (distanceToTargetThisMovement <= 0.5f) ? 1 : 0;

            bestPositionDistance[id.x] = (hasAgentCrashed[id.x] != 1 && improvesBestDistance) ? distanceToTargetThisMovement : bestPositionDistance[id.x];

            hasAgentCrashed[id.x] = hasIntersected ? 1 : 0;

            indexOfFirstCollision[id.x] = intersects && improves ? id.y : indexOfFirstCollision[id.x];

            indexOfLastAgentValidMovement[id.x] = intersects && improves ? id.y : indexOfLastAgentValidMovement[id.x];
            indexOfLastAgentValidMovement[id.x] = hasAgentCrashed[id.x] == 0 && hasAgentReachedTarget[id.x] == 1 ? id.y : indexOfLastAgentValidMovement[id.x];

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
        CalculatePopulationFitness();

        Database.AddIteration(new Database.Database_IterationEntry(currentSimulationIndex, RatioOfSuccess, SuccessfulAgents, CrashedAgents, 0, AverageFitness, MedianFitness, MaxFitness, MinFitness, VarianceFitness, StandardDeviationFitness));

        GetElites();
        if (Controller.Instance.numIterations % Controller.Settings.learningPeriod == 0)
        {
            currentElite.Clear();

            currentElite.AddRange(learningPeriodAccumulatedElite.GetRange(0, Controller.Settings.elitism));
            learningPeriodAccumulatedElite.Clear();
        }
        if (currentElite.Count == 0) //First learning period, initialize agents with random paths
        {
            for (int i = 0; i < numAgents; i++)
            {
                Dna currentAgentDna = new Dna();
                populationDna[i] = new EliteDna(currentAgentDna);
                for (int j = 0; j < numMovements - 1; j++)
                {
                    agentsPathLines[i * numMovements + j] = new Line(
                        currentAgentDna.Lines[j],
                        currentAgentDna.Lines[j + 1]);
                }

                hasAgentCrashed[i] = 0;
                hasAgentReachedTarget[i] = 0;
                collisionPoints[i] = Vector2.zero;
                indexOfFirstCollision[i] = numMovements - 1;
                indexOfLastAgentValidMovement[i] = numMovements - 1;
                bestPosition[i] = currentAgentDna.Lines[numMovements - 1];
                bestPositionDistance[i] = float.MaxValue;
            }
        }
        else
        {
            Selection();
            Reproduction();
            SetElites();
        }


        Controller.Instance.collisionsCPU = lastAgentValidPosition;
    }

    public void CalculatePopulationFitness()
    {
        float fitness = 0;
        for (int i = 0; i < numAgents; i++)
        {
            float distanceToTarget = CalculateDistance(lastAgentValidPosition[i]);
            if (distanceToTarget < 1)
            {
                distanceToTarget = 1;
            }

            float bestDistance = bestPositionDistance[i];
            if (bestDistance < 1)
            {
                bestDistance = 1;
            }

            fitness = 1000 * 1 / distanceToTarget * 1 / bestDistance;

            if (hasAgentCrashed[i] == 1)
                fitness *= .5f;
            if (hasAgentReachedTarget[i] == 1)
            {
                fitness *= 4;
                fitness *= (((numMovements - indexOfLastAgentValidMovement[i]) / (float)numMovements) + 1);
            }
            populationDna[i].fitness = fitness;
            //populationFitness[i] = fitness;              
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

    public void GetElites()
    {
        populationDna = populationDna.OrderByDescending(agent => agent.fitness).ToArray();
        for (int i = 0; i < Controller.Settings.elitism; i++)
        {
            learningPeriodAccumulatedElite.Add(new EliteDna(populationDna[i]));
        }
    }

    public void Selection()
    {
        matingPool.Clear();

        float maxFitness = currentElite.Max(agent => agent.fitness);

        for (int i = 0; i < currentElite.Count; i++)
        {
            float fitnessNormalized = Map(
                currentElite[i].fitness,
                0,
                maxFitness,
                0,
                1);
            int n = (int)fitnessNormalized * 100;
            for (int j = 0; j < n; j++)
            {
                matingPool.Add(currentElite[i]);
            }
        }
    }

    private float Map(float n, float start1, float stop1, float start2, float stop2)
    {
        return ((n - start1) / (stop1 - start1)) * (stop2 - start2) + start2;
    }

    public void Reproduction()
    {
        for (int i = 0; i < populationDna.Length; i++)
        {
            int index1 = Random.Range(0, matingPool.Count);
            int index2 = Random.Range(0, matingPool.Count);
            EliteDna parent1 = matingPool[index1];
            EliteDna parent2 = matingPool[index2];

            Dna child = parent1.dna.Crossover(parent2.dna);

            child.Mutate();

            populationDna[i] = new EliteDna(child);
            for (int j = 0; j < numMovements - 1; j++)
            {
                agentsPathLines[i * numMovements + j] = new Line(
                    child.Lines[j],
                    child.Lines[j + 1]);
            }
            hasAgentCrashed[i] = 0;
            hasAgentReachedTarget[i] = 0;
            collisionPoints[i] = Vector2.zero;
            indexOfFirstCollision[i] = numMovements - 1;
            indexOfLastAgentValidMovement[i] = numMovements - 1;
            bestPosition[i] = child.Lines[numMovements - 1];
            bestPositionDistance[i] = float.MaxValue;
        }
    }

    public void SetElites()
    {
        for (int i = 0; i < Controller.Settings.elitism; i++)
        {
            int index = Random.Range(0, numAgents);
            populationDna[index] = new EliteDna(currentElite[i].dna);
            for (int j = 0; j < numMovements - 1; j++)
            {
                agentsPathLines[index * numMovements + j] = new Line(
                    currentElite[i].dna.Lines[j],
                    currentElite[i].dna.Lines[j + 1]);
            }
            bestPosition[index] = currentElite[i].dna.Lines[numMovements - 1];
        }
    }
}
