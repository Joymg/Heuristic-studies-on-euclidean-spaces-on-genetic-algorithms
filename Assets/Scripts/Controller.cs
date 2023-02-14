using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum Map
{
    DiagonalObstacles,
    DiagonalObstacles1,
    DiagonalObstacles2,
    StraightObstacles,
    StraightObstacles1,
    StraightObstacles2,
}

[System.Serializable]
public struct MapSelection
{
    public Map mapEnum;
    public GameObject mapObject;
    public Transform spawn;
    public Transform target;
}

public class Controller : MonoBehaviour
{
    public static Controller Instance { get; set; }

    [System.Serializable]
    public struct Settings
    {
        public static int iterations = 100;
        public static int learningPeriod = 5;
        public static int populationSize = 50;
        public static int movements = 50;
        public static int elitism = 10;
        public static float mutationProb = 0.05f;
        public static float speed = 1f;
        public static TypeOfDistance typeOfDistance = 0;
        public static Map map = 0;
    }

    public int numAgents;
    public int numMovements;

    public int numIterations = 1;

    public float stopDuration;
    public float time;

    [Space]
    public bool useGPU = true;
    public bool useCPU = true;


    public Population population;

    public Transform spawn;
    public Transform target;

    public List<Obstacle> obstacles;
    public List<MapSelection> maps;

    public Action IncrementIteration;
    public Action AgentCrashed;


    public Vector2[] collisionsGPU;
    public Vector2[] collisionsCPU;

    private void Awake()
    {
        Instance = this;
        time = stopDuration;
        numIterations = 1;

        numAgents = Settings.populationSize;
        numMovements = Settings.movements;
        mutationChance = Settings.mutationProb;

        Time.timeScale = Settings.speed;

        MapSelection mapSelected = maps.Find(x => x.mapEnum == Settings.map);
        mapSelected.mapObject.SetActive(true);

        obstacles = FindObjectsOfType<Obstacle>().ToList();

        spawn = mapSelected.spawn;
        target = mapSelected.target;

        population.Initialize(Settings.populationSize, Settings.movements, Settings.mutationProb, Settings.typeOfDistance, spawn, target);


        Database.CreateDB();
        StartCoroutine(Wait());

    }

    private IEnumerator Wait()
    {
        yield return new WaitForSecondsRealtime(2f);
        CPUTester.isCalculating = false;
        GPUCalculator.isCalculating = false;
    }

    private void FixedUpdate()
    {
        if (useGPU)
        {
            if (!GPUCalculator.isCalculating)
            {
                GPUCalculator.Initialize(Settings.populationSize, Settings.movements, obstacles.Count, obstacles.ToArray());
                //StartCoroutine(Wait());
            }
        }

        if (useCPU)
        {
            if (!CPUTester.isCalculating)
            {
                CPUTester.Initialize(Settings.populationSize, Settings.movements, obstacles.Count, obstacles.ToArray());

               //StartCoroutine(Wait());
            }
        }

        if (population.IsRunning)
        {
            population.Tick();
        }
        else
        {
            if (numIterations >= Settings.iterations)
            {
                Time.timeScale = 1;
                while (time < stopDuration)
                {
                    time += Time.unscaledDeltaTime;
                    return;
                }
                time = 0;
                population.RepresentBest();
                return;
            }

            population.NextGeneration();

            IncrementIteration?.Invoke();
            numIterations++;

            CPUTester.isCalculating = false;
            GPUCalculator.isCalculating = false;
        }
    }

    private void OnDrawGizmos()
    {
        for (int i = 0; i < collisionsGPU.Length; i++)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(collisionsGPU[i], 0.2f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(collisionsCPU[i], 0.2f);
        }

        for (int i = 0; i < CPUTester.agentsPathLines.Length; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(CPUTester.agentsPathLines[i].u, CPUTester.agentsPathLines[i].v);
        }
    }
}
