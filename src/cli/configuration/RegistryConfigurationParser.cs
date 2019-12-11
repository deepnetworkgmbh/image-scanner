using System;
using System.IO;
using Serilog;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeResolvers;

namespace cli.configuration
{
    public class RegistryConfigurationParser
    {
        private static readonly ILogger Logger = Log.ForContext<RegistryConfigurationParser>();
        private static readonly IDeserializer Deserializer;

        private readonly Lazy<RegistryConfiguration> registryConfig;

        static RegistryConfigurationParser()
        {
            Deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .WithTypeResolver(new DynamicTypeResolver())
                .WithTagMapping("!registries", typeof(RegistryConfiguration))
                .Build();
        }

        public RegistryConfigurationParser(string configFilePath)
        {
            if (!File.Exists(configFilePath))
            {
                Logger.Fatal("Container registry config file does not exist at {ConfigFilePath}", configFilePath);
                throw new Exception($"Container registry config file does not exist at {configFilePath}");
            }

            this.registryConfig = new Lazy<RegistryConfiguration>(() => Init(configFilePath));
        }

        public RegistryConfiguration Get()
        {
            return this.registryConfig.Value;
        }

        private static RegistryConfiguration Parse(string input)
        {
            return Deserializer.Deserialize<RegistryConfiguration>(input);
        }

        private static RegistryConfiguration Init(string configFilePath)
        {
            var configString = File.ReadAllText(configFilePath);

            return Parse(configString);
        }
    }
}