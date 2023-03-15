using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public enum TypeOfDistance
{
    Euclidean,
    Manhattan,
    Chebyshev,
}

public class Population : MonoBehaviour
{
    private int numAgents;
    private int numMovements;
    [SerializeField]
    private float mutationChance;
    private int numEliteAgents = 3;
    private int generations = 0;

    private Vector2 spawnPoint;
    private Vector2 targetPoint;

    private List<Agent> population;
    //private List<Agent> matingPool;
    private List<EliteDna> matingPool;

    private TypeOfDistance typeOfDistance;
    private List<EliteDna> currentElite;
    private List<EliteDna> learningPeriodAccumulatedElite;

    public GameObject agentEuclideanPrefab;
    public GameObject agentManhattanPrefab;
    public GameObject agentChebyshevPrefab;

    public Action AgentArrivedToTarget;

    public TypeOfDistance TypeOfDistance => typeOfDistance;
    public int NumMovements => numMovements;

    private float TotalFitness => population.Sum(agent => agent.Fitness);

    public int SuccessfulAgents => population.Count(agent => agent.ReachedTarget);
    public int CrashedAgents => population.Count(agent => agent.HitObstacle);
    public float RatioOfSuccess => (float)SuccessfulAgents / numAgents;
    public float AverageFitness => population.Sum(agent => agent.fitness) / numAgents;
    public float MedianFitness
    {
        get
        {
            population.OrderBy(x => x.fitness);
            return numAgents % 2 == 0 ? (population[numAgents / 2].fitness + population[(int)(numAgents / 2) - 1].fitness) / 2 : population[numAgents / 2].fitness;
        }
    }

    public float StandardDeviationFitness
    {
        get
        {
            population.OrderBy(x => x.fitness);
            float sum = 0;
            for (int i = 0; i < numAgents; i++)
            {
                sum += population[i].fitness * population[i].fitness;
            }
            var avg = AverageFitness;
            return Mathf.Sqrt((sum / numAgents) - (avg * avg));
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

    public float MaxFitness => population.Max(agent => agent.fitness);
    public float MinFitness => population.Min(agent => agent.fitness);

    public bool IsRunning => population.Any(agent => !agent.Finished);

    public List<Agent> AgentList => population;

    public void Initialize(int numAgents, int numMovements, float mutationChance, TypeOfDistance typeOfDistance, Transform spawn, Transform target)
    {
        population = new List<Agent>();
        matingPool = new List<EliteDna>();
        learningPeriodAccumulatedElite = new List<EliteDna>();
        currentElite = new List<EliteDna>();

        this.numAgents = numAgents;
        this.numMovements = numMovements;
        this.mutationChance = mutationChance;

        this.typeOfDistance = typeOfDistance;

        spawnPoint = spawn.position;
        targetPoint = target.position;

        CreateNewGeneration();
    }

    public void CreateNewGeneration()
    {
        population = new List<Agent>(numAgents);

        GameObject agentContainer = new GameObject("Agent Container");
        for (int i = 0; i < numAgents; i++)
        {
            GameObject prefab;
            switch (typeOfDistance)
            {
                case TypeOfDistance.Euclidean:
                    prefab = Instantiate(agentEuclideanPrefab);
                    break;
                case TypeOfDistance.Manhattan:
                    prefab = Instantiate(agentManhattanPrefab);
                    break;
                case TypeOfDistance.Chebyshev:
                    prefab = Instantiate(agentChebyshevPrefab);
                    break;
                default:
                    prefab = Instantiate(agentEuclideanPrefab);
                    break;
            }
            Agent agent = prefab.GetComponent<Agent>();
            agent.Initialize(spawnPoint, targetPoint, new Dna());
            population.Add(agent);

            agent.transform.SetParent(agentContainer.transform);
        }
    }

    public void Tick()
    {
        if (Controller.Instance.simulationType == SimulationType.UnityPhysics)
        {
            for (int i = 0; i < population.Count; i++)
            {
                if (population[i].CheckTargetReached())
                {
                    AgentArrivedToTarget?.Invoke();
                }
                population[i].Tick();
            }
        }
    }

    public bool GenerationCompleted()
    {
        return population.Any(agent => agent.ReachedTarget);
    }

    public void NextGeneration()
    {
        CalculatePopulationFitness();
        //Database.AddIteration(new Database.Database_IterationEntry(Controller.Instance.numIterations, RatioOfSuccess, SuccessfulAgents, CrashedAgents, 0, AverageFitness, MedianFitness, MaxFitness, MinFitness, VarianceFitness, StandardDeviationFitness));
        GetElites();

        //Time to learn so we take the numElites best from the nextElites list (calculate fitness first), save them in currentElite,
        //clear next elite and then continue with the next generation as normal
        if (Controller.Instance.numIterations % Controller.Settings.learningPeriod == 0)
        {
            Debug.Log("aprendo");
            //CalculateNextElitesFitness();
            currentElite.Clear();

            currentElite.AddRange(learningPeriodAccumulatedElite.GetRange(0, Controller.Settings.elitism));
            learningPeriodAccumulatedElite.Clear();
        }

        if (currentElite.Count == 0) //First learning period, initialize agents with random paths
        {
            Debug.Log("random");
            foreach (Agent agent in population)
            {
                agent.Initialize(spawnPoint, targetPoint, new Dna());
            }
        }
        else
        {
            Selection();
            Reproduction();
            SetElites();
        }
    }

    public void CalculatePopulationFitness()
    {
        foreach (Agent agent in population)
        {
            agent.CalculateFitness();
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

    public void GetElites()
    {
        OrderPopulation();

        for (int i = 0; i < Controller.Settings.elitism; i++)
        {
            learningPeriodAccumulatedElite.Add(new EliteDna(population[i]));
        }
        return;
    }

    private void OrderPopulation()
    {
        population = population.OrderByDescending(agent => agent.fitness).ToList();
    }

    public void Reproduction()
    {
        for (int i = 0; i < population.Count; i++)
        {
            int index1 = Random.Range(0, matingPool.Count);
            int index2 = Random.Range(0, matingPool.Count);
            EliteDna parent1 = matingPool[index1];
            EliteDna parent2 = matingPool[index2];

            Dna child = parent1.dna.Crossover(parent2.dna);

            child.Mutate();

            population[i].Initialize(spawnPoint, targetPoint, child);
            population[i].renderer.color = Color.red;
            population[i].renderer.sortingOrder = 2;
        }
    }

    public void SetElites()
    {
        for (int i = 0; i < Controller.Settings.elitism; i++)
        {
            int index = Random.Range(0, numAgents);
            population[index].Initialize(spawnPoint, targetPoint, currentElite[i].dna);
            population[index].renderer.color = Color.green;
            population[index].renderer.sortingOrder = 3;
        }
    }


    private float Map(float n, float start1, float stop1, float start2, float stop2)
    {
        return ((n - start1) / (stop1 - start1)) * (stop2 - start2) + start2;
    }

    private float GetMaxFitness()
    {
        return population.Max(agent => agent.Fitness);
    }


    public void RepresentBest()
    {
        OrderPopulation();
        if (population.Count > 1)
        {
            for (int i = numAgents - 1; i >= 1; i--)
            {
                Destroy(population[i].gameObject);
                population.RemoveAt(i);
            }
        }

        population[0].Initialize(spawnPoint, targetPoint, population[0].Dna);
    }
}

