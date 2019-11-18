using System;
using System.IO;

using Microsoft.Extensions.Configuration;

using Serilog;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeResolvers;

namespace webapp.Configuration
{
    /// <summary>
    /// Parse YAML based application configuration.
    /// </summary>
    public class ConfigurationParser
    {
        private static readonly ILogger Logger = Log.ForContext<ConfigurationParser>();
        private static readonly IDeserializer Deserializer;

        private readonly Lazy<KubeScannerConfiguration> kubeScannerConfig;

        static ConfigurationParser()
        {
            Deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeResolver(new DynamicTypeResolver())
                .WithTagMapping("!trivy-scanner", typeof(TrivyConfiguration))
                .WithTagMapping("!file-exporter", typeof(FileExporterConfiguration))
                .WithTagMapping("!file-importer", typeof(FileImporterConfiguration))
                .Build();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationParser"/> class.
        /// </summary>
        /// <param name="configuration">The application configuration object.</param>
        public ConfigurationParser(IConfiguration configuration)
        {
            const string envVarName = "KUBE_SCANNER_CONFIG_FILE_PATH";
            var configFilePath = configuration[envVarName];

            if (string.IsNullOrEmpty(configFilePath))
            {
                Logger.Fatal("There is no KUBE_SCANNER_CONFIG_FILE_PATH environment variable with Kube Scanner config filepath");
                throw new Exception(envVarName);
            }

            if (!File.Exists(configFilePath))
            {
                Logger.Fatal("Kube Scanner config file does not exist at {ConfigFilePath}", configFilePath);
                throw new Exception($"Kube Scanner config file does not exist at {configFilePath}");
            }

            this.kubeScannerConfig = new Lazy<KubeScannerConfiguration>(() => this.Init(configFilePath));
        }

        /// <summary>
        /// Parse the string into dotnet object.
        /// </summary>
        /// <param name="input">String representation of YAML file.</param>
        /// <returns>The application configuration object.</returns>
        public static KubeScannerConfiguration Parse(string input)
        {
            return Deserializer.Deserialize<KubeScannerConfiguration>(input);
        }

        /// <summary>
        /// Parses Scanner Config on the first request and cache the result in memory.
        /// </summary>
        /// <returns>Kube Scanner configuration object.</returns>
        public KubeScannerConfiguration Get()
        {
            return this.kubeScannerConfig.Value;
        }

        // Parse the application configuration file.
        private KubeScannerConfiguration Init(string configFilePath)
        {
            var configString = File.ReadAllText(configFilePath);

            return Parse(configString);
        }
    }
}