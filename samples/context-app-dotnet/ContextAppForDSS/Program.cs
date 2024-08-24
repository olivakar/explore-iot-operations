// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using ContextAppForDSS;
using System.Security.Cryptography.X509Certificates;


namespace ContextualDataIngestor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddConsole());
            ILogger logger = factory.CreateLogger("Program");
            DataSourceType dataSourceType = Enum.TryParse<DataSourceType>(Environment.GetEnvironmentVariable("ENDPOINT_TYPE"),
               true,
               out var parsedType)
                   ? parsedType
                   : throw new ArgumentException("Invalid or missing ENDPOINT_TYPE environment variable");

            Dictionary<string, string> parameters = CreateParametersFromEnvironmentVariables();

            Console.WriteLine("testing");
            string clientCertFile = parameters["ClientCertFilePath"];
            string clientKeyFile = parameters["ClientCertKeyFilePath"];
            string keyPassword = parameters["ClientKeyPassword"] ?? string.Empty;

            Console.WriteLine("Certificate Path");
            Console.WriteLine(clientCertFile);
            Console.WriteLine("Key path");
            Console.WriteLine(clientKeyFile);
            Console.WriteLine("Password txt");
            Console.WriteLine(keyPassword);

            Console.WriteLine("Certificate contents after file read:");
            Console.WriteLine(File.ReadAllText(clientCertFile));

            Console.WriteLine("Key contents after file read:");
            Console.WriteLine(File.ReadAllText(clientKeyFile));

            try
            {
                Console.WriteLine("CreateFromEncryptedPem");
                X509Certificate2.CreateFromEncryptedPem(File.ReadAllText(clientCertFile), File.ReadAllText(clientKeyFile), keyPassword);
                Console.WriteLine("Certificate created successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating certificate: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            try
            {
                Console.WriteLine("CreateFromEncryptedPem after reading contents");
                string certContents = File.ReadAllText(clientCertFile);
                string keyContents = clientKeyFile is null ? certContents : File.ReadAllText(clientKeyFile);
                X509Certificate2.CreateFromEncryptedPem(certContents, keyContents, keyPassword);
                Console.WriteLine("Certificate created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating certificate: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            try
            {
                Console.WriteLine("CreateFromEncryptedPemFile");
                X509Certificate2.CreateFromEncryptedPemFile(clientCertFile, keyPassword, clientKeyFile);
                Console.WriteLine("Certificate created successfully");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating certificate: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }



            using IDataRetriever dataRetriever = DataRetrieverFactory.CreateDataRetriever(dataSourceType, parameters);

            ContextualDataOperation operation = new ContextualDataOperation(dataRetriever, parameters, logger);
            await operation.PopulateContextualDataAsync();
        }

        public static Dictionary<string, string> CreateParametersFromEnvironmentVariables()
        {
            var parameters = new Dictionary<string, string>
            {
                { "HttpBaseURL", Environment.GetEnvironmentVariable("HTTP_BASE_URL") ?? string.Empty },
                { "HttpPath", Environment.GetEnvironmentVariable("HTTP_PATH") ?? string.Empty },
                { "AuthType", Environment.GetEnvironmentVariable("AUTH_TYPE") ?? string.Empty },
                { "HttpUsername", Environment.GetEnvironmentVariable("HTTP_USERNAME") ?? string.Empty },
                { "HttpPassword", Environment.GetEnvironmentVariable("HTTP_PASSWORD") ?? string.Empty },
                { "IntervalSecs", Environment.GetEnvironmentVariable("REQUEST_INTERVAL_SECONDS") ?? string.Empty },
                { "DssKey", Environment.GetEnvironmentVariable("DSS_KEY") ?? string.Empty },
                { "MqttHost", Environment.GetEnvironmentVariable("MQTT_HOST") ?? string.Empty },
                { "MqttClientId", Environment.GetEnvironmentVariable("MQTT_CLIENT_ID") ?? "someClientId"},
                { "SqlServerName",  Environment.GetEnvironmentVariable("SQL_SERVER_NAME") ?? string.Empty },
                { "SqlDatabaseName",  Environment.GetEnvironmentVariable("SQL_DB_NAME") ?? string.Empty },
                { "SqlTableName",  Environment.GetEnvironmentVariable("SQL_TABLE_NAME") ?? string.Empty },
                { "SqlUsername",  Environment.GetEnvironmentVariable("SQL_USERNAME") ?? "sa" },
                { "SqlPassword",  Environment.GetEnvironmentVariable("SQL_PASSWORD") ?? string.Empty },
                { "UseTls", Environment.GetEnvironmentVariable("USE_TLS") ?? "false"},
                { "SatTokenPath", Environment.GetEnvironmentVariable("SAT_TOKEN_PATH") ?? string.Empty},
                { "CaFilePath", Environment.GetEnvironmentVariable("CA_FILE_PATH") ?? string.Empty},
                { "ClientCertFilePath", Environment.GetEnvironmentVariable("CLIENT_CERT_FILE") ?? string.Empty},
                { "ClientCertKeyFilePath", Environment.GetEnvironmentVariable("CLIENT_KEY_FILE") ?? string.Empty},
                { "ClientKeyPassword", Environment.GetEnvironmentVariable("CLIENT_KEY_PASSWORD") ?? string.Empty},
            };

            return parameters;
        }
    }
}