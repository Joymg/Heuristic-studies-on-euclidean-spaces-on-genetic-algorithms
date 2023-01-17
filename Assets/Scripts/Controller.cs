using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public static Controller Instance { get; set; }

    public struct Settings
    {
        public static int generations = 10;
        public static int populationSize = 50;
        public static int movements = 15;
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
        lifecycle = 0;
        recordTime = float.MaxValue;
        Instance = this;

        obstacles = FindObjectsOfType<Obstacle>().ToList();

        population.Initialize(numAgents, numMovements, mutationChance, spawn, target);
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
            population.CalculateFitness();
            population.Selection();
            population.Reproduction();
            population.SetElites(numIterations);
            numIterations++;
        }
    }
}
