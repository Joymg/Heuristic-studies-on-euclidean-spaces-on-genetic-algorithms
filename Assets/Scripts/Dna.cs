using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dna
{
    public List<Vector2> Genes;
    public List<Vector2> Lines;

    public Dna(Dna copydna)
    {
        Genes = new List<Vector2>(copydna.Genes);
    }

    public Dna(List<Vector2> newGenes = null)
    {
        if (newGenes != null)
        {
            Genes = newGenes.ToList();
        }
        else
        {
            Genes = new List<Vector2>();
            for (int i = 0; i < Controller.Instance.numMovements; i++)
            {
                Genes.Add(new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized );
            }
        }

        GenerateGPUData();
    }



    // CROSSOVER
    // Creates new DNA sequence from two (this & and a partner)
    public Dna Crossover(Dna partner)
    {
        List<Vector2> child = new List<Vector2>();
        int padding = (int)Mathf.Floor(Controller.Instance.numMovements * 0.2f);

        int crossoverIndex = Random.Range(0 + padding, Controller.Instance.numMovements - padding);

        for (int i = 0; i < Controller.Instance.numMovements; i++)
        {
            if (i > crossoverIndex)
                child.Add(Genes[i]);
            else
                child.Add(partner.Genes[i]);
        }

        Dna newGenes = new Dna(child);
        return newGenes;
    }

    public void Mutate()
    {
        for (int i = 0; i < Controller.Instance.numMovements; i++)
        {
            if (Random.Range(0, 1f) < Controller.Instance.mutationChance)
            {
                Genes[i] = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized ;
            }
        }
    } 
    
    private void GenerateGPUData()
    {
        Lines = new List<Vector2>();
        Vector2 initialPosition = (Vector2)Controller.Instance.spawn.transform.position;
        for (int i = 0; i < Genes.Count; i++)
        {
            initialPosition = initialPosition + Genes[i];
            Lines.Add(initialPosition);
        }
    }
}

[System.Serializable]
public class EliteDna
{
    public EliteDna(Agent agent)
    {
        dna = agent.Dna;
        fitness = agent.fitness;
    }

    public Dna dna;
    public float bestDistance;
    public float distanceToTarget;
    public bool hitObstacle;
    public bool arrivedToTarget;

    public float normalizedBestDistance;
    public float normalizedDistanceToTarget;

    public float fitness;
}