using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;
using UnityEngine;

public static class Database
{
    private static SqliteConnection connection;
    private static SqliteCommand command;

    public struct Database_SimulationEntry
    {
        private readonly TypeOfDistance _distanceType;
        private readonly int _startingNumAgents;
        private readonly int _elitism;
        private readonly float _mutationChance;
        private readonly int _iterationID;

        public Database_SimulationEntry(TypeOfDistance distanceType, int startingNumAgents, int elitism,
            float mutationChance, float parentMutationWeight, bool usesPoisson, int iterationID)
        {
            this._distanceType = distanceType;
            this._startingNumAgents = startingNumAgents;
            this._elitism = elitism;
            this._mutationChance = mutationChance;
            this._iterationID = iterationID;
        }

        public override string ToString()
        {
            string typeName = "";
            switch (_distanceType)
            {
                case TypeOfDistance.Manhattan:
                    typeName = "Manhattan";
                    break;

                case TypeOfDistance.Euclidean:
                    typeName = "Euclidean";
                    break;

                case TypeOfDistance.Chebyshev:
                    typeName = "Chebyshev";
                    break;
            }

            return
                $"INSERT INTO simulations (distanceType, startingNumAgents, elitism, mutationChance, firstSuccessfulIteration)VALUES ('{typeName}', '{_startingNumAgents}', '{_elitism}', '{_mutationChance}', '{_iterationID}');";
        }
    }

    public struct Database_IterationEntry
    {
        private readonly int _currentIteration;
        private readonly float _successRatio;
        private readonly int _numSuccessfulAgents;
        private readonly int _numCrashedAgents;
        private readonly int _milliseconds;
        private readonly float _averageFitness;
        private readonly float _maxFitness;

        public Database_IterationEntry(int currentIteration, float successRatio, int numSuccessfulAgents,
            int numCrashedAgents, int milliseconds, float averageFitness, float maxFitness)
        {
            _currentIteration = currentIteration;
            _successRatio = successRatio;
            _numSuccessfulAgents = numSuccessfulAgents;
            _numCrashedAgents = numCrashedAgents;
            _milliseconds = milliseconds;
            _averageFitness = averageFitness;
            _maxFitness = maxFitness;
        }

        public override string ToString()
        {
            return
                $"INSERT INTO iterations (currentIteration, successRatio, numSuccessfulAgents, numCrashedAgents, milliseconds, averageFitness, maxFitness) VALUES " +
                $"('{_currentIteration}', '{_successRatio}', '{_numSuccessfulAgents}','{_numSuccessfulAgents}', '{_milliseconds}','{_averageFitness}','{_maxFitness}');";
        }
    }

    private const string randomIndividualsDBName = "URI=file:Simulation.db";
    public static List<Agent> BestRandomAgents = new List<Agent>();

    public static void SaveBestRandomAgents(List<Agent> agents)
    {
        for (int i = 0; i < agents.Count; i++)
        {
            BestRandomAgents.Add(agents[i]);
        }

        BestRandomAgents.Sort((agentA, agentB) => agentA.Fitness.CompareTo(agentB.Fitness));
        BestRandomAgents =
            BestRandomAgents.GetRange(0, Mathf.Min(BestRandomAgents.Count, Controller.Settings.elitism));
    }


    public static void CreateDB()
    {
        connection = new SqliteConnection(randomIndividualsDBName);
        connection.Open();

        command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys=on;";
        command.ExecuteNonQuery();

        command.CommandText = "DROP TABLE iterations;";
        command.ExecuteNonQuery();
        command.CommandText = "DROP TABLE simulations;";
        command.ExecuteNonQuery();

        command.CommandText = "CREATE TABLE IF NOT EXISTS iterations (iterationID INTEGER PRIMARY KEY AUTOINCREMENT," +
                              "currentIteration INTEGER," +
                              "successRatio REAL," +
                              "numSuccessfulAgents INTEGER," +
                              "numCrashedAgents INTEGER," +
                              "milliseconds INTEGER," +
                              "averageFitness REAL," +
                              "maxFitness REAL" +
                              ");";

        command.ExecuteNonQuery();

        command.CommandText =
            "CREATE TABLE IF NOT EXISTS simulations (simulationID INTEGER PRIMARY KEY AUTOINCREMENT," +
            "distanceType VARCHAR(255) CHECK(distanceType = 'Euclidean' OR distanceType = 'Manhattan' OR distanceType = 'Chebyshev')," +
            "startingNumAgents INTEGER," +
            "elitism INTEGER," +
            "mutationChance REAL," +
            "parentMutationWeight REAL CHECK(parentMutationWeight >= 0 OR parentMutationWeight <= 1)," +
            "usesPoisson BOOLEAN NOT NULL CHECK (usesPoisson IN (0, 1))," +
            "firstSuccessfulIteration INTEGER," +
            "FOREIGN KEY (firstSuccessfulIteration) REFERENCES iterations(iterationID)" +
            ");";
        command.ExecuteNonQuery();
    }

    public static void AddSimulation(Database_SimulationEntry simulationEntry)
    {
        command.CommandText = simulationEntry.ToString();
        command.ExecuteNonQuery();
    }

    public static void AddIteration(Database_IterationEntry iterationEntry)
    {
        command.CommandText = iterationEntry.ToString();
        command.ExecuteNonQuery();
    }
}