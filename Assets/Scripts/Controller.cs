using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

public enum SimulationType
{
    UnityPhysics,
    CPUMath,
    GPUMath,
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
        public static int learningPeriod = 1;
        public static int populationSize = 50;
        public static int movements = 50;
        public static int elitism = 10;
        public static float mutationProb = 0.05f;
        public static float speed = 1f;
        public static TypeOfDistance typeOfDistance = 0;
        public static Map map = 0;
        public static SimulationType simualtionType = SimulationType.UnityPhysics;
        public static int numberOfSimulations = 1;
    }

    public int numAgents;
    public int numMovements;

    public int numIterations = 1;

    public float mutationChance;

    public float stopDuration;
    public float time;

    [SerializeField] private int SavedSimulations;

    [Space]
    public SimulationType simulationType;

    public CPUTester cpuTester;
    //public GPUCalculator gpuCalculator;


    public Population population;

    public Transform spawn;
    public Transform target;

    public List<Obstacle> obstacles;
    public List<MapSelection> maps;

    public Action IncrementIteration;
    public Action AgentCrashed;


    public Vector2[] collisionsGPU;
    public Vector2[] collisionsCPU;


    public Stopwatch simStopwatch;
    public Stopwatch iteStopwatch;
    private bool addedSimulation;


    private void Awake()
    {
        Instance = this;
        time = stopDuration;
        numIterations = 1;

        numAgents = Settings.populationSize;
        numMovements = Settings.movements;
        mutationChance = Settings.mutationProb;
        Time.timeScale = Settings.speed;
        simulationType = Settings.simualtionType;

        MapSelection mapSelected = maps.Find(x => x.mapEnum == Settings.map);
        mapSelected.mapObject.SetActive(true);

        obstacles = FindObjectsOfType<Obstacle>().ToList();

        spawn = mapSelected.spawn;
        target = mapSelected.target;

        //population.Initialize(Settings.populationSize, Settings.movements, Settings.mutationProb, Settings.typeOfDistance, spawn, target);
        //cpuTester.Initialize(Settings.populationSize, Settings.iterations, Settings.movements, obstacles.Count, obstacles.ToArray(), Settings.typeOfDistance, target.transform.position, spawn.transform.position);

        Database.CreateDB($"{Settings.populationSize}_" +
            $"{Settings.iterations}_" +
            $"{Settings.movements}_" +
            $"{Settings.elitism}_" +
            $"{Settings.learningPeriod}_" +
            $"{Settings.mutationProb * 100}_" +
            $"{Settings.typeOfDistance}_" +
            $"{Settings.simualtionType}_Simulation.db");

        simStopwatch = new Stopwatch();
        iteStopwatch = new Stopwatch();


        switch (simulationType)
        {
            case SimulationType.UnityPhysics:
                population.Initialize(Settings.populationSize, Settings.movements, Settings.typeOfDistance, spawn, target);
                simStopwatch.Start();
                iteStopwatch.Start();
                break;
            case SimulationType.CPUMath:
                cpuTester.Initialize(Settings.populationSize, Settings.numberOfSimulations, Settings.iterations, Settings.movements, obstacles.Count, obstacles.ToArray(), Settings.typeOfDistance, target.transform.position, spawn.transform.position);
                break;
            case SimulationType.GPUMath:
                //gpuCalculator.Initialize(Settings.populationSize, Settings.movements, obstacles.Count, obstacles.ToArray());
                break;
        }

        StartCoroutine(Wait());

        SavedSimulations = Database.GetNumSimulationsInDatabase();

        Database.CloseConnection();

    }

    private IEnumerator Wait()
    {
        yield return new WaitForSecondsRealtime(2f);
        cpuTester.isCalculating = false;
        GPUCalculator.isCalculating = false;
    }

    private void FixedUpdate()
    {
        //if (useGPU)
        //{
        //    if (!GPUCalculator.isCalculating)
        //    {
        //        GPUCalculator.Initialize(Settings.populationSize, Settings.movements, obstacles.Count, obstacles.ToArray());
        //        //StartCoroutine(Wait());
        //    }
        //}

        //if (useCPU)
        //{
        //    if (!cpuTester.isCalculating)
        //    {
        //        cpuTester.Initialize(Settings.populationSize, Settings.iterations, Settings.movements, obstacles.Count, obstacles.ToArray(), Settings.typeOfDistance, target.transform.position, spawn.transform.position);

        //        //StartCoroutine(Wait());
        //    }
        //}

        if (simulationType == SimulationType.UnityPhysics)
        {

            if (population.IsRunning)
            {
                population.Tick();
            }
            //else
            //{
            //    cpuTester.CalculatePopulationFitness();
            //    population.CalculatePopulationFitness();
            //}

            if (!population.IsRunning)//&& canDoNextIteration)
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
                    if (!addedSimulation)
                    {
                        addedSimulation = true;
                        Database.AddIteration(new Database.Database_IterationEntry(numIterations,
                                                                           population.RatioOfSuccess,
                                                                           population.SuccessfulAgents,
                                                                           population.CrashedAgents,
                                                                           (int)iteStopwatch.ElapsedMilliseconds,
                                                                           population.AverageFitness,
                                                                           population.MedianFitness,
                                                                           population.MaxFitness,
                                                                           population.MinFitness,
                                                                           population.VarianceFitness,
                                                                           population.StandardDeviationFitness));
                        Database.AddSimulation(new Database.Database_SimulationEntry(Settings.typeOfDistance, Settings.populationSize, Settings.movements, Settings.elitism, Settings.mutationProb, Settings.map, (int)simStopwatch.ElapsedMilliseconds));
                        simStopwatch.Stop();
                        iteStopwatch.Stop();
                    }
                    return;
                }

                population.NextGeneration();

                Database.AddIteration(new Database.Database_IterationEntry(numIterations,
                                                                           population.RatioOfSuccess,
                                                                           population.SuccessfulAgents,
                                                                           population.CrashedAgents,
                                                                           (int)iteStopwatch.ElapsedMilliseconds,
                                                                           population.AverageFitness,
                                                                           population.MedianFitness,
                                                                           population.MaxFitness,
                                                                           population.MinFitness,
                                                                           population.VarianceFitness,
                                                                           population.StandardDeviationFitness));


                IncrementIteration?.Invoke();
                numIterations++;
                iteStopwatch.Restart();

                //cpuTester.isCalculating = false;
                //GPUCalculator.isCalculating = false;
                //canDoNextIteration = false;
            }
        }
    }


    private void OnDrawGizmos()
    {
        for (int i = 0; i < cpuTester.agentsPathLines.Length; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(cpuTester.agentsPathLines[i].u, cpuTester.agentsPathLines[i].v);
        }
        for (int i = 0; i < collisionsGPU.Length; i++)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(collisionsCPU[i], 0.2f);
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(collisionsGPU[i], 0.1f);
        }
    }
}
