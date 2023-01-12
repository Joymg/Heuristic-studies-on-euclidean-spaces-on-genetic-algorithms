using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{

    private float speed = 20;
    private float finishTime = 0;
    private float recordDistance = int.MaxValue;
    private Dna dna;
    public float fitness = 0;

    private int geneIndex = 0;

    private bool hitObstacle;
    private bool reachedTarget;
    private bool outOfEnergy;
    public bool ReachedTarget => reachedTarget;

    public bool Finished => hitObstacle || reachedTarget || outOfEnergy;

    private Vector2 target ;
    private Vector2 nextStepPosition;

    public float Fitness => fitness;
    public Dna Dna => dna;

    public void Initialize(Vector2 spawn, Vector2 target, Dna dna)
    {
        Reset();
        this.dna = dna;
        transform.position = spawn;
        this.target = Controller.Instance.target.transform.position;

        nextStepPosition = (Vector2)transform.position + dna.Genes[geneIndex];
    }

    private void Reset()
    {
        geneIndex = 0;
        hitObstacle = reachedTarget = outOfEnergy = false;
        fitness = 0;
        finishTime = 0;
        recordDistance = int.MaxValue;
    }

    public void CalculateFitness()
    {
        if (recordDistance < 1) recordDistance = 1;

        fitness = 1 / (finishTime * recordDistance);

        fitness = Mathf.Pow(fitness, 2);

        if (hitObstacle)
            fitness *= .1f;
        if (reachedTarget)
            fitness *= 2f;
    }

    public void Tick()
    {
        if (hitObstacle || reachedTarget || outOfEnergy) return;
        if (geneIndex == Controller.Instance.numMovements)
        {
            outOfEnergy = true;
            return;
        }
        
        if ((Vector2)transform.position == nextStepPosition)
        {
            nextStepPosition = (Vector2)transform.position + dna.Genes[geneIndex];
            geneIndex++;
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, nextStepPosition, speed * Time.deltaTime);
        }
    }



    public void CheckTargetReached()
    {
        float distance = Vector2.Distance(transform.position, target);

        if (distance < recordDistance) recordDistance = distance;

        if (distance < 0.5f && !reachedTarget)
        {
            reachedTarget = true;
            CalculateFitness();
        }
        else if (!reachedTarget)
        {
            finishTime++;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 6)
        {
            hitObstacle = true;
        }
    }
}
