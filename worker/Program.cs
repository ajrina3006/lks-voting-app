using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Npgsql;
using StackExchange.Redis;

namespace Worker
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var redisConn = OpenRedisConnection("master.lks-redis.h0hlgw.use1.cache.amazonaws.com:6379");
                var redis = redisConn.GetDatabase();

                var pgsql = OpenDbConnection("Host=lks-rds.cnznixf4cggg.us-east-1.rds.amazonaws.com;Username=admin;Password=LKSNCC2024;Database=postgres");

                var keepAliveCommand = pgsql.CreateCommand();
                keepAliveCommand.CommandText = "SELECT 1";

                var definition = new { vote = "", voter_id = "" };
                while (true)
                {
                    Thread.Sleep(100);
                    if (redisConn == null || !redisConn.IsConnected) {
                        Console.WriteLine("Reconnecting Redis");
                        redisConn = OpenRedisConnection("master.lks-redis.h0hlgw.use1.cache.amazonaws.com:6379");
                        redis = redisConn.GetDatabase();
                    }
                    string json = redis.ListLeftPopAsync("votes").Result;
                    if (json != null)
                    {
                        var vote = JsonConvert.DeserializeAnonymousType(json, definition);
                        Console.WriteLine($"Processing vote for '{vote.vote}' by '{vote.voter_id}'");
                        if (!pgsql.State.Equals(ConnectionState.Open))
                        {
                            Console.WriteLine("Reconnecting DB");
                            pgsql = OpenDbConnection("Host=lks-rds.cnznixf4cggg.us-east-1.rds.amazonaws.com;Username=admin;Password=LKSNCC2024;Database=postgres");
                        }
                        else
                        {
                            UpdateVote(pgsql, vote.voter_id, vote.vote);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        static ConnectionMultiplexer OpenRedisConnection(string hostname)
        {
            bool isConnected = false;
            ConnectionMultiplexer connection = null;
            while (!isConnected)
            {
                try
                {
                    Console.Error.WriteLine("Connecting to redis");
                    connection = ConnectionMultiplexer.Connect(hostname);
                    isConnected = true;
                }
                catch (RedisConnectionException)
                {
                    Console.Error.WriteLine("Waiting for redis");
                    Thread.Sleep(1000);
                }
            }
            return connection;
        }

        static IDbConnection OpenDbConnection(string connectionString)
        {
            IDbConnection connection = null;
            while (connection == null)
            {
                try
                {
                    connection = new NpgsqlConnection(connectionString);
                    Console.WriteLine("Opened database connection");
                    connection.Open();
                    CreateVotesTable(connection);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.ToString());
                    Console.Error.WriteLine("Waiting for db");
                    Thread.Sleep(1000);
                    connection = null;
                }
            }
            return connection;
        }

        static void CreateVotesTable(IDbConnection connection)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"CREATE TABLE IF NOT EXISTS votes (
                                        id VARCHAR(255) NOT NULL UNIQUE,
                                        vote VARCHAR(255) NOT NULL
                                    )";
            command.ExecuteNonQuery();
        }

        static void UpdateVote(IDbConnection connection, string voterId, string vote)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"INSERT INTO votes (id, vote) VALUES (@id, @vote)
                                        ON CONFLICT ON CONSTRAINT votes_id_key
                                        DO UPDATE SET vote = EXCLUDED.vote";
            ((NpgsqlCommand)command).Parameters.AddWithValue("@id", voterId);
            ((NpgsqlCommand)command).Parameters.AddWithValue("@vote", vote);
            command.ExecuteNonQuery();
        }

        static string GetIp(string hostname)
        {
            return hostname;
        }
    }
}
