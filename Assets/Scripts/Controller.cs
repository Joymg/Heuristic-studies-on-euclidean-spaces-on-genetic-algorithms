using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public enum Map
{
    DiagonalObstacles,
    DiagonalObstacles1,
    DiagonalObstacles2,
    StraightObstacles,
    StraightObstacles1,
    StraightObstacles2,
}

[System.Serializable]
public struct MapSelection
{
    public Map mapEnum;
    public GameObject mapObject;
    public Transform spawn;
    public Transform target;
}

public class Controller : MonoBehaviour
{
    public static Controller Instance { get; set; }

    [System.Serializable]
    public struct Settings
    {
        public static int iterations = 100;
        public static int learningPeriod = 5;
        public static int populationSize = 50;
        public static int movements = 50;
        public static int elitism = 10;
        public static float mutationProb = 0.05f;
        public static float speed = 4f;
        public static TypeOfDistance typeOfDistance = 0;
        public static Map map = 0;
    }

    public int numAgents;
    public int numMovements;

    public int numIterations = 1;

    public float stopDuration;
    public float time;

    [Space]
    public float mutationChance = 0.05f;

    public Population population;

    public Transform spawn;
    public Transform target;

    public List<Obstacle> obstacles;
    public List<MapSelection> maps;

    public Action IncrementIteration;
    public Action AgentCrashed;


    [Header("Temp")]
    public int[] collides;
    public Transform A;
    public Transform B;
    public Vector2[] LineA;
    public Vector2[] LineB;
    public Obs[] obstacle;
    public Obstacle obs;
    private ComputeShader computeShader;

    [System.Serializable]
    public struct Obs
    {
        public Vector2 x;
        public Vector2 y;
        public Vector2 z;
        public Vector2 w;
    }
    private void Awake()
    {
        Instance = this;
        time = stopDuration;
        numIterations = 1;

        obstacles = FindObjectsOfType<Obstacle>().ToList();
        Time.timeScale = Settings.speed;

        MapSelection mapSelected = maps.Find(x => x.mapEnum == Settings.map);
        mapSelected.mapObject.SetActive(true);
        spawn = mapSelected.spawn;
        target = mapSelected.target;

        population.Initialize(Settings.populationSize, Settings.movements, Settings.mutationProb, Settings.typeOfDistance, spawn, target);


        Database.CreateDB();
        StartCoroutine(Wait());

    }

    private IEnumerator Wait()
    {
        yield return new WaitForSecondsRealtime(2f);
    }

    private void FixedUpdate()
    {
        //CPUTester.Calculate(LineA[0], LineB[0], obstacle[0]);
        //ComputeShader();

        if (population.IsRunning)
        {
            population.Tick();
        }
        else
        {
            if (numIterations >= Settings.iterations)
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

            population.NextGeneration();

            IncrementIteration?.Invoke();
            numIterations++;
        }
    }

    private void ComputeShader()
    {
        int boolID = Shader.PropertyToID("collides");
        int obstacleID = Shader.PropertyToID("obstacle");
        int lineAID = Shader.PropertyToID("lineA");
        int lineBID = Shader.PropertyToID("lineB");

        computeShader = Resources.Load<ComputeShader>("ComputeShaders/Physics");
        int index = computeShader.FindKernel("LineCollides");

        ComputeBuffer lineABuffer = new ComputeBuffer(1, sizeof(float) * 2);
        lineABuffer.SetData(LineA);
        computeShader.SetBuffer(index, lineAID, lineABuffer);

        ComputeBuffer lineBBuffer = new ComputeBuffer(1, sizeof(float) * 2);
        lineBBuffer.SetData(LineB);
        computeShader.SetBuffer(index, lineBID, lineBBuffer);

        ComputeBuffer ObstacleBuffer = new ComputeBuffer(1, sizeof(float) * 2 * 4);
        ObstacleBuffer.SetData(obstacle);
        computeShader.SetBuffer(index, obstacleID, ObstacleBuffer);

        ComputeBuffer boolBuffer = new ComputeBuffer(1, sizeof(int));
        boolBuffer.SetData(collides);
        computeShader.SetBuffer(index, boolID, boolBuffer);

        computeShader.Dispatch(index, 1, 1, 1);

        boolBuffer.GetData(collides);

        lineABuffer.Dispose();
        lineBBuffer.Dispose();
        ObstacleBuffer.Dispose();
        boolBuffer.Dispose();

        Debug.Log($"GPU: {collides[0]}");
        collides[0] = 0;
    }

    [ContextMenu("SetObstacle")]
    public void Obstacle()
    {
        obstacle[0] = new Obs
        {
            x = obs.vertex[0],
            y = obs.vertex[1],
            z = obs.vertex[2],
            w = obs.vertex[3]
        };
    }

    [ContextMenu("SetVectors")]
    public void Vector()
    {
        LineA[0] = A.position;
        LineB[0] = B.position;
    }

    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.red;
    //    Gizmos.DrawSphere(A.position, .1f);
    //    Gizmos.DrawSphere(B.position, .1f);
    //    Gizmos.DrawLine(A.position, B.position);
    //}
}
