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
    private List<Agent> matingPool;

    public TypeOfDistance typeOfDistance;
    private List<Dna> elitePool;

    public GameObject agentEuclideanPrefab;
    public GameObject agentManhattanPrefab;
    public GameObject agentChebyshevPrefab;

    private float maxTagetDistance;
    private float minTargetDistance;
    private float maxBestDistance;
    private float minBestDistance;

    public Action AgentArrivedToTarget;

    public int NumMovements=> numMovements;

    private float TotalFitness => population.Sum(agent => agent.Fitness);

    public int SuccessfulAgents => population.Count(agent => agent.ReachedTarget);
    public int CrashedAgents => population.Count(agent => agent.HitObstacle);
    public float RatioOfSuccess => (float) SuccessfulAgents / numAgents;
    public float AverageFitness => population.Sum(agent => agent.fitness) / numAgents;
    public float MaxFitness => population.Max(agent => agent.fitness);

    public bool IsRunning => population.Any(agent => !agent.Finished);

    public void Initialize(int numAgents, int numMovements, float mutationChance, TypeOfDistance typeOfDistance ,Transform spawn, Transform target)
    {
        population = new List<Agent>();
        matingPool = new List<Agent>();
        elitePool = new List<Dna>();

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
        for (int i = 0; i < population.Count; i++)
        {
            if (population[i].CheckTargetReached())
            {
                AgentArrivedToTarget?.Invoke();
            }
            population[i].Tick();
        }
    }

    public bool TargetReached()
    {
        return population.Any(agent => agent.ReachedTarget);
    }

    public void CalculateFitness()
    {
        population = population.OrderBy((x) => x.DistanceToTarget).ToList();
        minTargetDistance = population[0].DistanceToTarget;
        maxTagetDistance = population[population.Count - 1].DistanceToTarget;

        population = population.OrderBy((x) => x.BestDistance).ToList();
        minBestDistance = population[0].BestDistance;
        maxBestDistance = population[population.Count - 1].BestDistance;
        string fitnes = "";
        foreach (Agent agent in population)
        {
            agent.NormalizeFinalDistance(maxTagetDistance, minTargetDistance);
            agent.NormalizeBestDistance(maxBestDistance, minBestDistance);
            agent.CalculateFitness();
            fitnes += agent.fitness + " ";
        }
        //Debug.Log(fitnes);
    }

    public void Selection()
    {
        matingPool.Clear();

        float maxFitness = GetMaxFitness();

        for (int i = 0; i < population.Count; i++)
        {
            float fitnessNormalized = Map(
                population[i].Fitness,
                0,
                maxFitness,
                0,
                1);
            int n = (int)fitnessNormalized * 100;
            for (int j = 0; j < n; j++)
            {
                matingPool.Add(population[i]);
            }
        }
    }

    public void GetElites()
    {
        OrderPopulation();
        elitePool.Clear();
        for (int i = 0; i < Controller.Settings.elitism; i++)
        {
            switch (typeOfDistance)
            {
                case TypeOfDistance.Euclidean:
                    elitePool.Add(population[i].Dna);
                    break;
                case TypeOfDistance.Manhattan:
                    elitePool.Add(population[i].Dna);
                    break;
                case TypeOfDistance.Chebyshev:
                    elitePool.Add(population[i].Dna);
                    break;
            }
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

            Agent parent1 = matingPool[index1];
            Agent parent2 = matingPool[index2];

            Dna child = parent1.Dna.Crossover(parent2.Dna);

            child.Mutate();

            population[i].Initialize(spawnPoint, targetPoint, child);
            population[i].renderer.color = Color.red;
            population[i].renderer.sortingOrder = 0;
        }
    }

    public void SetElites(int iteration)
    {
        for (int i = 0; i < Controller.Settings.elitism; i++)
        {
            int index = Random.Range(0, numAgents);
            population[index].Initialize(spawnPoint, targetPoint, elitePool[i]);
            population[index].gameObject.name = $"Best in {iteration}";
            population[index].renderer.color = Color.green;
            population[index].renderer.sortingOrder = 1;
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

