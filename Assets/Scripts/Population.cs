using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Population : MonoBehaviour
{
    private int numAgents;
    private int numMovements;
    private float mutationChance;
    private int generations = 0;

    private Vector2 spawnPoint;
    private Vector2 targetPoint;

    private List<Agent> population;
    private List<Agent> matingPool;

    public GameObject agentPrefab;

    private float TotalFitness => population.Sum(agent => agent.Fitness);
    public bool IsRunning => population.Any(agent => !agent.Finished);

    public void Initialize(int numAgents, int numMovements, float mutationChance, Transform spawn, Transform target)
    {
        population = new List<Agent>();
        matingPool = new List<Agent>();

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
        for (int i = 0; i < numAgents; i++)
        {
            GameObject prefab = Instantiate(agentPrefab);
            Agent agent = prefab.GetComponent<Agent>();
            agent.Initialize(spawnPoint, targetPoint, new Dna());
            population.Add(agent);
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

    public void Reproduction()
    {
        for (int i = 0; i < population.Count; i++)
        {
            int index1 = UnityEngine.Random.Range(0, matingPool.Count);
            int index2 = UnityEngine.Random.Range(0, matingPool.Count);

            Agent parent1 = matingPool[index1];
            Agent parent2 = matingPool[index2];

            Dna child = parent1.Dna.Crossover(parent2.Dna);

            child.Mutate();

            population[i].Initialize(spawnPoint, targetPoint, child);
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
}
