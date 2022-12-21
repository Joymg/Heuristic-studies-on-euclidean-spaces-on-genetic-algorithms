using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dna
{
    public List<Vector2> Genes;

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
                Genes.Add(new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * .5f);
            }
        }
    }

    // CROSSOVER
    // Creates new DNA sequence from two (this & and a partner)
    public Dna Crossover(Dna partner)
    {
        List<Vector2> child = new List<Vector2>();

        int crossoverIndex = (int)Random.Range(0, Controller.Instance.numMovements);

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
                Genes[i] = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * .5f;
            }
        }
    }
}
