using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{

    private float speed = 20;
    private float finishTime = 0;
    private Dna dna;
    public float fitness = 0;

    private int geneIndex = 0;

    private bool hitObstacle;
    private bool reachedTarget;
    private bool outOfEnergy;

    private float bestDistance = float.MaxValue;
    private float distanceToTarget;
    private float normalizedFinalDistance;
    private float normalizedBestDistance;
    public bool ReachedTarget => reachedTarget;
    public bool HitObstacle => hitObstacle;
    public float DistanceToTarget => distanceToTarget;
    public float BestDistance => bestDistance;

    public bool Finished => hitObstacle || reachedTarget || outOfEnergy;

    protected Vector2 target;
    private Vector2 nextStepPosition;

    public SpriteRenderer renderer;

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
        distanceToTarget = 0;
        bestDistance = float.MaxValue;
    }

    public void CalculateFitness()
    {
        fitness = 100 * normalizedFinalDistance * normalizedBestDistance;

        if (hitObstacle)
            fitness *= .5f;
        if (reachedTarget)
            fitness *= 4f;
    }

    protected virtual float CalculateDistance()
    {
        return -1;
    }

    public void Tick()
    {
        if (hitObstacle || reachedTarget || outOfEnergy) return;
        if (geneIndex == Controller.Instance.numMovements)
        {
            distanceToTarget = CalculateDistance();
            outOfEnergy = true;
            return;
        }

        if ((Vector2)transform.position == nextStepPosition)
        {
            nextStepPosition = (Vector2)transform.position + dna.Genes[geneIndex];
            geneIndex++;
            float distanceToTarget = CalculateDistance();
            if (distanceToTarget < bestDistance)
            {
                bestDistance = distanceToTarget;
            }
        }
        else
        {
            transform.position = Vector2.MoveTowards(transform.position, nextStepPosition, speed * Time.deltaTime);
        }
    }



    public bool CheckTargetReached()
    {
        float distance = Vector2.Distance(transform.position, target);

        if (distance < 0.5f && !reachedTarget)
        {
            reachedTarget = true;
            distanceToTarget = CalculateDistance();
            return true;
            //CalculateFitness();
        }
        else if (!reachedTarget)
        {
            finishTime++;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 6)
        {
            hitObstacle = true;
            Controller.Instance.AgentCrashed?.Invoke();
            distanceToTarget = CalculateDistance();
        }
    }

    public void NormalizeFinalDistance(float maxDistance, float minDistance)
    {
        normalizedFinalDistance = 1 - ((distanceToTarget - minDistance) / (maxDistance - minDistance));
    }

    public void NormalizeBestDistance(float maxDistance, float minDistance)
    {
        normalizedBestDistance = 1 - ((bestDistance - minDistance) / (maxDistance - minDistance));
    }
}
