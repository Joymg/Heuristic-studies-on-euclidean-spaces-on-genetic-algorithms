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

    private float TotalFitness => population.Sum(agent => agent.Fitness);
    public bool IsRunning => population.Any(agent => !agent.Finished);

    public void Initialize(int numAgents, int numMovements, float mutationChance, Transform spawn, Transform target)
    {
        population = new List<Agent>();
        matingPool = new List<Agent>();
        elitePool = new List<Dna>();

        this.numAgents = numAgents;
        this.numMovements = numMovements;
        this.mutationChance = mutationChance;

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
            population[i].CheckTargetReached();
            population[i].Tick();
        }
    }

    public bool TargetReached()
    {
        return population.Any(agent => agent.ReachedTarget);
    }

    public void CalculateFitness()
    {
        population.ForEach(agent => agent.CalculateFitness());
    }

    public void Selection()
    {
        matingPool.Clear();

        GetElites();

        float maxFitness = GetMaxFitness();

        for (int i = 0; i < population.Count; i++)
        {
            float fitnessNormal = Map(
                population[i].Fitness,
                0,
                maxFitness,
                0,
                1);
            int n = (int)fitnessNormal * 100;
            for (int j = 0; j < n; j++)
            {
                matingPool.Add(population[i]);
            }
        }
    }

    private void GetElites()
    {
        population = population.OrderByDescending(agent => agent.fitness).ToList();
        SaveElites();
        //elitePool = new List<Agent>(population.GetRange(0, numEliteAgents));
        string s = "";

        for (int i = 0; i < numAgents; i++)
        {
            s += $"{population[i].fitness}, ";
        }

        Debug.Log(s);
        s = "";

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
        for (int i = 0; i < 3; i++)
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

    private void SaveElites()
    {
        elitePool.Clear();
        for (int i = 0; i < numEliteAgents; i++)
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
}

