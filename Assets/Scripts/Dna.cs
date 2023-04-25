using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

[System.Serializable]
public class Dna
{
    public List<Vector2> Genes;
    public List<Vector2> Lines;

    public Dna(Dna copydna)
    {
        Genes = new List<Vector2>(copydna.Genes);
        Lines = new List<Vector2>(copydna.Lines);
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
                Genes.Add(new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized);
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

        int crossoverIndex = UnityEngine.Random.Range(0 + padding, Controller.Instance.numMovements - padding);

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
            if (UnityEngine.Random.Range(0, 1f) < Controller.Instance.mutationChance)
            {
                Genes[i] = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f)).normalized;
            }
        }

        GenerateGPUData();
    }

    private void GenerateGPUData()
    {
        Lines = new List<Vector2>();
        Vector2 initialPosition = (Vector2)Controller.Instance.spawn.transform.position;
        Lines.Add(initialPosition);
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
        guid = Guid.NewGuid();
        dna = new Dna(agent.Dna);
        fitness = agent.fitness;
    }

    public EliteDna(EliteDna eliteDna)
    {
        guid = eliteDna.guid;
        dna = new Dna(eliteDna.dna);
        fitness = eliteDna.fitness;
    }

    public EliteDna(Dna dna)
    {
        guid = Guid.NewGuid();
        this.dna = new Dna(dna);
    }

    Guid guid;
    public Dna dna;
    public float fitness;
}