using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Controller : MonoBehaviour
{
    public static Controller Instance { get; set; }

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
        }
    }
}
