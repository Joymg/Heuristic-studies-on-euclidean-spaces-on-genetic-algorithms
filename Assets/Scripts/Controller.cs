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
        public static int movements = 50;
        public static int elitism = 10;
        public static float mutationProb = 0.05f;
        public static float speed = 4f;
    }

    public int numAgents;
    public int numMovements;

    public int numIterations;

    private float lifecycle;
    private float recordTime;

    public float stopDuration;
    public float time;

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
        time = stopDuration;

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
            if (numIterations > Settings.generations)
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
            population.Selection();
            population.GetElites();
            population.Reproduction();
            population.SetElites(numIterations);
            numIterations++;
        }
    }
}
