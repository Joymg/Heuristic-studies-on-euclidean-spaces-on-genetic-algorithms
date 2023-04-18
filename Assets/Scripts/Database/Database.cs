using System;
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
        private readonly int _startingNumMovements;
        private readonly int _elitism;
        private readonly float _mutationChance;
        private readonly Map _mapID;

        public Database_SimulationEntry(TypeOfDistance distanceType, int startingNumAgents, int startingNumMovements, int elitism, float mutationChance, Map mapID)
        {
            _distanceType = distanceType;
            _startingNumAgents = startingNumAgents;
            _startingNumMovements = startingNumMovements;
            _elitism = elitism;
            _mutationChance = mutationChance;
            _mapID = mapID;
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

            string mapName = "";
            switch (_mapID)
            {
                case Map.DiagonalObstacles:
                    mapName = "DiagonalObstacles";
                    break;
                case Map.DiagonalObstacles1:
                    mapName = "DiagonalObstacles1";
                    break;
                case Map.DiagonalObstacles2:
                    mapName = "DiagonalObstacles2";
                    break;
                case Map.StraightObstacles:
                    mapName = "StraightObstacles";
                    break;
                case Map.StraightObstacles1:
                    mapName = "StraightObstacles1";
                    break;
                case Map.StraightObstacles2:
                    mapName = "StraightObstacles2";
                    break;
            }

            return
                $"INSERT INTO simulations (distanceType, startingNumAgents, startingNumMovements, elitism, mutationChance, mapID)VALUES ('{typeName}', '{_startingNumAgents}', '{_startingNumMovements}', '{_elitism}', '{_mutationChance}', '{mapName}');";
        }
    }

    public struct Database_IterationEntry
    {
        private readonly int _simulationID;
        private readonly float _successRatio;
        private readonly int _numSuccessfulAgents;
        private readonly int _numCrashedAgents;
        private readonly int _milliseconds;
        private readonly float _averageFitness;
        private readonly float _medianFitness;
        private readonly float _maxFitness;
        private readonly float _minFitness;
        private readonly float _varianceFitness;
        private readonly float _standardDeviationFitness;

        public Database_IterationEntry(int simulationID, float successRatio, int numSuccessfulAgents,
            int numCrashedAgents, int milliseconds, float averageFitness, float medianFitness,
            float maxFitness, float minFitness, float varianceFitness, float standardDeviationFitness)
        {
            _simulationID = simulationID;
            _successRatio = successRatio;
            _numSuccessfulAgents = numSuccessfulAgents;
            _numCrashedAgents = numCrashedAgents;
            _milliseconds = milliseconds;
            _averageFitness = averageFitness;
            _medianFitness = medianFitness;
            _maxFitness = maxFitness;
            _minFitness = minFitness;
            _varianceFitness = varianceFitness;
            _standardDeviationFitness = standardDeviationFitness;
        }

        public override string ToString()
        {
            return
                $"INSERT INTO iterations (simulation, successRatio, numSuccessfulAgents, numCrashedAgents, milliseconds, averageFitness, medianFitness, maxFitness, minFitness, varianceFitness, standardDeviationFitness) VALUES " +
                $"('{_simulationID}', '{_successRatio}', '{_numSuccessfulAgents}','{_numCrashedAgents}', '{_milliseconds}','{_averageFitness}','{_medianFitness}','{_maxFitness}','{_minFitness}','{_varianceFitness}','{_standardDeviationFitness}');";
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


        command.CommandText = "CREATE TABLE IF NOT EXISTS simulations (simulationID INTEGER PRIMARY KEY AUTOINCREMENT," +
                              "distanceType VARCHAR(255) CHECK(distanceType = 'Euclidean' OR distanceType = 'Manhattan' OR distanceType = 'Chebyshev')," +
                              "startingNumAgents INTEGER," +
                              "startingNumMovements INTEGER," +
                              "elitism INTEGER," +
                              "mutationChance REAL," +
                              "mapID VARCHAR(255) CHECK(mapID = 'DiagonalObstacles' OR mapID = 'DiagonalObstacles1' OR mapID = 'DiagonalObstacles2' OR mapID = 'StraightObstacles' OR mapID = 'StraightObstacles1' OR mapID = 'StraightObstacles2')" +
                              ");";
        command.ExecuteNonQuery();
        command.CommandText = "CREATE TABLE IF NOT EXISTS iterations (iterationID INTEGER PRIMARY KEY AUTOINCREMENT," +
                              "simulation INTEGER," +
                              "successRatio REAL," +
                              "numSuccessfulAgents INTEGER," +
                              "numCrashedAgents INTEGER," +
                              "milliseconds INTEGER," +
                              "averageFitness REAL," +
                              "medianFitness REAL," +
                              "maxFitness REAL," +
                              "minFitness REAL," +
                              "varianceFitness REAL," +
                              "standardDeviationFitness REAL," +
                              "FOREIGN KEY (simulation) REFERENCES simulations(simulationID)" +
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

    public static int GetNumSimulationsInDatabse()
    {
        command.CommandText = "SELECT COUNT(*) FROM simulations;";
        SqliteDataReader reader = command.ExecuteReader();
        reader.Read();
        int numSimulations = Convert.ToInt32(reader.GetValue(0)); 
        reader.Close();

        return numSimulations;
    }
}