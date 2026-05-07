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
                // Ganti ke endpoint ElastiCache
                var redisConn = OpenRedisConnection("master.lks-redis.h0hlgw.use1.cache.amazonaws.com:6379");
                var redis = redisConn.GetDatabase();

                // Ganti ke endpoint RDS
                var pgsql = OpenDbConnection("Host=lks-rds.cnznixf4cggg.us-east-1.rds.amazonaws.com;Username=admin;Password=LKSNCC2024;Database=postgres");

                var keepAliveCommand = pgsql.CreateCommand();
                keepAliveCommand.CommandText = "SELECT 1";

                var definition = new { vote = "", voter_id = "" };
                while (true)
                {
                    Thread.Sleep(100);
                    if (redisConn == null || !redisConn.IsConnected) {
                        Console.WriteLine("Reconnecting Redis");
                        redisConn = OpenRedisConnection("master.lks-redis.h0hlgw.use1.cache.amazonaws.com:6379"); // ← sama
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
