using System.IO;

using FluentAssertions;

using NUnit.Framework;

using webapp.Configuration;

namespace kube_scanner.tests
{
    [TestFixture]
    public class YamlConfigTests
    {
        [Test]
        public void HappyPath()
        {
            // Arrange
            var stringConfig = File.ReadAllText("./kube-scanner.sample.yaml");

            // Act
            var config = ConfigurationParser.Parse(stringConfig);

            // Assert
            config.Parallelization.Should().Be(10);
            config.KubeConfigPath.Should().NotBeNullOrEmpty();
            config.Exporter.Should().BeOfType<FileExporterConfiguration>();
            config.Importer.Should().BeOfType<FileImporterConfiguration>();
            config.Scanner.Should().BeOfType<TrivyConfiguration>();
            config.Scanner.As<TrivyConfiguration>().BinaryPath.Should().NotBeNullOrWhiteSpace();
            config.Scanner.As<TrivyConfiguration>().CachePath.Should().NotBeNullOrWhiteSpace();
            config.Scanner.As<TrivyConfiguration>().Registries.Should().HaveCount(2);
        }
    }
}
