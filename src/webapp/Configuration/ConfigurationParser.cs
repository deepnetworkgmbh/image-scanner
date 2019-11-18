using System;
using System.IO;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Core;

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

        private readonly string configFilePath;

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

            if (string.IsNullOrEmpty(configuration[envVarName]))
            {
                Logger.Fatal("There is no KUBE_SCANNER_CONFIG_FILE_PATH environment variable with Kube Scanner config filepath");
                throw new Exception(envVarName);
            }

            this.configFilePath = configuration[envVarName];

            if (!File.Exists(this.configFilePath))
            {
                Logger.Fatal("Kube Scanner config file does not exist at {ConfigFilePath}", this.configFilePath);
                throw new Exception($"Kube Scanner config file does not exist at {this.configFilePath}");
            }
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
        /// Parse the application configuration file.
        /// </summary>
        /// <returns>Kube Scanner configuration object.</returns>
        public async Task<KubeScannerConfiguration> Parse()
        {
            var configString = await File.ReadAllTextAsync(this.configFilePath);

            return Parse(configString);
        }
    }
}