﻿using ContextualDataIngestor;
using System;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Microsoft.Extensions.Logging;

namespace ContextualDataIngestor
{
    internal class SqlDataRetriever : IDataRetriever
    {
        private string _connectionString;
        private string _query;
        private readonly IAuthStrategy _authConfig;
        //ILogger logger;

        public SqlDataRetriever(SqlRetrievalConfig retrievalConfig, IAuthStrategy authConfig)
        {
            _connectionString = $"Server={retrievalConfig.ServerName};Database={retrievalConfig.DatabaseName};";
            _query = $"SELECT * FROM {retrievalConfig.TableName}";
            _authConfig = authConfig;
            //using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            //ILogger logger = factory.CreateLogger("SqlDataRetriever");
            
        }

        private void Authenticate()
        {
            if (_authConfig.GetType() == typeof(SqlBasicAuth))
            {
                SqlBasicAuth basicAuth = (SqlBasicAuth)_authConfig;
                Console.WriteLine($"Auth Username: {basicAuth.Username}");
                Console.WriteLine($"Auth Pass: {basicAuth.Password}");
                //string username = Encoding.UTF8.GetString(Convert.FromBase64String(basicAuth.Username));
                //string password = Encoding.UTF8.GetString(Convert.FromBase64String(basicAuth.Password));
                _connectionString = _connectionString + $"User Id={basicAuth.Username};Password={basicAuth.Password};";
                Console.WriteLine($"Conn Str: {_connectionString}");
                //Console.WriteLine($"Username: {username}");
                //Console.WriteLine($"Pass: {password}");
            }
        }

        public async Task<string> RetrieveDataAsync()
        {
            Authenticate();
            // StringBuilder to accumulate the formatted strings
            StringBuilder result = new StringBuilder();

            // Create a SQL connection object
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    // Open the connection to the database asynchronously
                    await connection.OpenAsync();
                    Console.WriteLine("Connected to the database.");

                    // Create a SQL command object
                    using (SqlCommand command = new SqlCommand(_query, connection))
                    {
                        // Execute the query and retrieve the data using a data reader asynchronously
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            // Check if there are rows to read
                            if (reader.HasRows)
                            {
                                // Read each row and format the data as a JSON-like string
                                while (await reader.ReadAsync())
                                {
                                    string formattedRow = $"{{ \"country\" : \"{reader["Country"]}\" , \"viscosity\" : {reader["Viscosity"]}, \"sweetness\" : {reader["Sweetness"]}, \"particle_size\" : {reader["ParticleSize"]}, \"overall\" : {reader["Overall"]} }}";
                                    result.AppendLine(formattedRow);
                                }
                            }
                            else
                            {
                                result.AppendLine("No data found.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            // Output the formatted result
            return result.ToString();
        }



        public void Dispose()
        {
            // No resources to dispose as of now
        }
    }
}