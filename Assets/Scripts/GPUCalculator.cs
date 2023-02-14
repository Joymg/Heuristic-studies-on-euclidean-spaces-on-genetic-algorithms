

using UnityEngine;

public static class GPUCalculator
{
    [System.Serializable]
    public struct ObstacleGPU
    {
        public Vector2 a;
        public Vector2 b;
        public Vector2 c;
        public Vector2 d;

        public static explicit operator ObstacleGPU(Obstacle obstacle)
        {
            return new ObstacleGPU()
            {
                a = obstacle.vertex[0],
                b = obstacle.vertex[1],
                c = obstacle.vertex[2],
                d = obstacle.vertex[3],
            };
        }
    }

    public struct LineGPU
    {
        public Vector2 u;
        public Vector2 v;

        public LineGPU(Vector2 u, Vector2 v)
        {
            this.u = u;
            this.v = v;
        }
    }



    private static ComputeShader computeShader;

    public static int numAgents;
    public static int numMovements;
    public static int numObstacles;

    public static bool isCalculating;

    public static LineGPU[] agentsPathLines;
    public static Vector2[] collisionPoints;

    public static ObstacleGPU[] obstaclesArray;

    public static int[] hasAgentCrashed;
    public static int[] indexOfFirstCollision;

    public static Vector2[] lastAgentValidPosition;

    public static readonly int lineAID = Shader.PropertyToID("lineA"),
        lineBID = Shader.PropertyToID("lineB"),
        numAgentsID = Shader.PropertyToID("num_movements"),
        numMovementsID = Shader.PropertyToID("num_agents"),
        numObstaclesID = Shader.PropertyToID("num_obstacle"),
        hasAgentCrashedID = Shader.PropertyToID("hasAgentCrashed"),
        obstaclesID = Shader.PropertyToID("obstacles"),
        agentsPathLinesID = Shader.PropertyToID("agentsPathLines"),
        collisionPointsID = Shader.PropertyToID("collisionPoints"),
        indexOfFirstCollisionID = Shader.PropertyToID("indexOfFirstCollision"),
        lastAgentValidPositionID = Shader.PropertyToID("lastAgentValidPosition");



    public static Transform A;
    public static Transform B;
    public static Vector2[] LineA;
    public static Vector2[] LineB;

    public static void Initialize(int population, int movements, int obstacles, Obstacle[] mapObstacles)
    {
        isCalculating = true;

        numAgents = population;
        numMovements = movements;
        numObstacles = obstacles;

        computeShader = Resources.Load<ComputeShader>("ComputeShaders/Physics");

        agentsPathLines = new LineGPU[population * movements];
        obstaclesArray = new ObstacleGPU[obstacles];

        hasAgentCrashed = new int[population];
        collisionPoints = new Vector2[population];
        indexOfFirstCollision = new int[population];
        lastAgentValidPosition = new Vector2[population];

        for (int i = 0; i < population; i++)
        {
            Dna currentAgentDna = Controller.Instance.population.AgentList[i].Dna;
            for (int j = 0; j < movements - 1; j++)
            {
                agentsPathLines[i * movements + j] = new LineGPU(currentAgentDna.Lines[j], currentAgentDna.Lines[j + 1]);
            }

            hasAgentCrashed[i] = 0;
            collisionPoints[i] = Vector2.zero;
            indexOfFirstCollision[i] = movements - 1;
            lastAgentValidPosition[i] = currentAgentDna.Lines[movements - 1];
        }

        for (int i = 0; i < obstacles; i++)
        {
            obstaclesArray[i] = (ObstacleGPU)mapObstacles[i];
        }

        Calculate();
    }


    private static void Calculate()
    {
        int intSize = sizeof(int);
        int floatSize = sizeof(float);
        int vec2Size = floatSize * 2;
        int lineSize = vec2Size * 2;
        int obstacleSize = vec2Size * 4;

        int lineCollisionKernelID = computeShader.FindKernel("LineCollides");

        /*ComputeBuffer lineABuffer = new ComputeBuffer(1, sizeof(float) * 2);
        lineABuffer.SetData(LineA);
        computeShader.SetBuffer(lineCollisionKernelID, lineAID, lineABuffer);

        ComputeBuffer lineBBuffer = new ComputeBuffer(1, sizeof(float) * 2);
        lineBBuffer.SetData(LineB);
        computeShader.SetBuffer(lineCollisionKernelID, lineBID, lineBBuffer);*/

        ComputeBuffer agentPathLinesBuffer = new ComputeBuffer(numAgents * numMovements, lineSize);
        agentPathLinesBuffer.SetData(agentsPathLines);
        computeShader.SetBuffer(lineCollisionKernelID, agentsPathLinesID, agentPathLinesBuffer);

        ComputeBuffer collisionPointsBuffer = new ComputeBuffer(numAgents, vec2Size);
        collisionPointsBuffer.SetData(collisionPoints);
        computeShader.SetBuffer(lineCollisionKernelID, collisionPointsID, collisionPointsBuffer);

        ComputeBuffer lastAgentValidPositionsBuffer = new ComputeBuffer(numAgents, vec2Size);
        lastAgentValidPositionsBuffer.SetData(lastAgentValidPosition);
        computeShader.SetBuffer(lineCollisionKernelID, lastAgentValidPositionID, lastAgentValidPositionsBuffer);

        ComputeBuffer obstaclesBuffer = new ComputeBuffer(numObstacles, obstacleSize);
        obstaclesBuffer.SetData(obstaclesArray);
        computeShader.SetBuffer(lineCollisionKernelID, obstaclesID, obstaclesBuffer);

        ComputeBuffer hasAgentCrashedBuffer = new ComputeBuffer(numAgents, intSize);
        hasAgentCrashedBuffer.SetData(hasAgentCrashed);
        computeShader.SetBuffer(lineCollisionKernelID, hasAgentCrashedID, hasAgentCrashedBuffer);

        ComputeBuffer indexOfFirstCollisionBuffer = new ComputeBuffer(numAgents, intSize);
        indexOfFirstCollisionBuffer.SetData(indexOfFirstCollision);
        computeShader.SetBuffer(lineCollisionKernelID, indexOfFirstCollisionID, indexOfFirstCollisionBuffer);

        computeShader.SetInt(numAgentsID, numAgents);
        computeShader.SetInt(numMovementsID, numMovements);
        computeShader.SetInt(numObstaclesID, numObstacles);

        computeShader.Dispatch(lineCollisionKernelID,
            Mathf.CeilToInt(numAgents / 8f),
            Mathf.CeilToInt(numMovements / 8f),
            Mathf.CeilToInt(numObstacles / 8f));

        hasAgentCrashedBuffer.GetData(hasAgentCrashed);
        collisionPointsBuffer.GetData(collisionPoints);
        lastAgentValidPositionsBuffer.GetData(lastAgentValidPosition);
        indexOfFirstCollisionBuffer.GetData(indexOfFirstCollision);

        /*lineABuffer.Dispose();
        lineBBuffer.Dispose();*/
        agentPathLinesBuffer.Dispose();
        collisionPointsBuffer.Dispose();
        lastAgentValidPositionsBuffer.Dispose();
        obstaclesBuffer.Dispose();
        hasAgentCrashedBuffer.Dispose();
        indexOfFirstCollisionBuffer.Dispose();


        Controller.Instance.collisionsGPU = lastAgentValidPosition;

    }

}
