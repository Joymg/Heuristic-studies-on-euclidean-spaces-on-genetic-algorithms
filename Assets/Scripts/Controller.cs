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
        public static int generations = 100;
        public static int populationSize = 50;
        public static int movements = 50;
        public static int elitism = 10;
        public static float mutationProb = 0.05f;
        public static float speed = 4f;
        public static TypeOfDistance typeOfDistance = 0;
        public static Map map = 0;
    }

    public int numAgents;
    public int numMovements;

    public int numIterations;

    private int lifecycle;
    private float recordTime;

    public float stopDuration;
    public float time;

    [Space]
    public float mutationChance = 0.05f;

    public Population population;

    public Transform spawn;
    public Transform target;

    public List<Obstacle> obstacles;
    public List<MapSelection> maps;

    public Action IncrementIteration;
    public Action AgentCrashed;

    private void Awake()
    {
        Instance = this;
        lifecycle = 0;
        recordTime = float.MaxValue;
        time = stopDuration;

        obstacles = FindObjectsOfType<Obstacle>().ToList();
        Time.timeScale = Settings.speed;

        MapSelection mapSelected = maps.Find(x => x.mapEnum == Settings.map);
        mapSelected.mapObject.SetActive(true);
        spawn = mapSelected.spawn;
        target = mapSelected.target;

        population.Initialize(Settings.populationSize, Settings.movements, Settings.mutationProb, Settings.typeOfDistance, spawn, target);


        Database.CreateDB();
        StartCoroutine(Wait());

    }

    private IEnumerator Wait()
    {
        yield return new WaitForSecondsRealtime(2f);
    }

    private void FixedUpdate()
    {
        if (population.IsRunning)
        {
            population.Tick();
            if (population.TargetReached() && lifecycle < recordTime)
            {
                recordTime = lifecycle;
            }
            lifecycle++;
        }
        else
        {
            if (numIterations >= Settings.generations)
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
            population.CalculateFitness();
            Database.AddIteration(new Database.Database_IterationEntry(numIterations, population.RatioOfSuccess, population.SuccessfulAgents, population.CrashedAgents, lifecycle, population.AverageFitness, population.MaxFitness));
            population.Selection();
            population.GetElites();
            population.Reproduction();
            population.SetElites(numIterations);
            IncrementIteration?.Invoke();
            numIterations++;
        }
    }
}
