using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public static Controller Instance { get; set; }

    [System.Serializable]
    public struct Settings
    {
        public static int generations = 10;
        public static int populationSize = 50;
        public static int movements = 15;
        public static int elitism = 3;
        public static float mutationProb = 0.05f;
        public static float speed = 4f;
    }

    public int numAgents;
    public int numMovements;

    public int numIterations;

    private float lifecycle;
    private float recordTime;

    

    [Space]
    public float mutationChance = 0.05f;

    public Population population;

    public Transform spawn;
    public Transform target;

    public List<Obstacle> obstacles;

    private void Start()
    {
        Instance = this;
        lifecycle = 0;
        recordTime = float.MaxValue;


        obstacles = FindObjectsOfType<Obstacle>().ToList();
        Time.timeScale = Settings.speed;

        population.Initialize(Settings.populationSize, Settings.movements, Settings.mutationProb, spawn, target);

        StartCoroutine(Wait());

    }

    private IEnumerator Wait()
    {
        yield return new WaitForSecondsRealtime(2f);
    }

    private void FixedUpdate()
    {
        if (numIterations)
        {

        }

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
            population.CalculateFitness();
            population.Selection();
            population.SaveElites();
            population.Reproduction();
            population.SetElites(numIterations);
            numIterations++;
        }
    }
}
