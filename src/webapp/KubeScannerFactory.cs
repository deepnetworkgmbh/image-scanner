using System;
using System.Linq;

using core.exporters;
using core.images;
using core.importers;
using core.scanners;

using webapp.Configuration;

namespace webapp
{
    /// <summary>
    /// Helps instantiating correct Scanner, Exporter and Importer implementations based on the application configuration.
    /// </summary>
    public class KubeScannerFactory
    {
        private readonly ConfigurationParser configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="KubeScannerFactory"/> class.
        /// </summary>
        /// <param name="configuration">the application configuration.</param>
        public KubeScannerFactory(ConfigurationParser configuration)
        {
            this.configuration = configuration;
        }

        /// <summary>
        /// Gets scanner implementation.
        /// </summary>
        /// <returns>At the moment, only trivy is supported, so returns Trivy scanner object.</returns>
        public IScanner GetScanner()
        {
            var scannerConfiguration = this.configuration.Get();

            switch (scannerConfiguration.Scanner)
            {
                case TrivyConfiguration trivy:

                    // TODO: init all available registries
                    var registry = trivy.Registries.FirstOrDefault();

                    return new Trivy(trivy.CachePath)
                    {
                        ContainerRegistryAddress = registry?.Address,
                        ContainerRegistryUserName = registry?.Username,
                        ContainerRegistryPassword = registry?.Password,
                        TrivyBinaryPath = trivy.BinaryPath,
                    };
                default:
                    throw new NotImplementedException("At the moment only trivy scanner is supported");
            }
        }

        /// <summary>
        /// Gets exporter implementation.
        /// </summary>
        /// <returns>At the moment, only file is supported, so returns File Exporter object.</returns>
        public IExporter GetExporter()
        {
            var scannerConfiguration = this.configuration.Get();

            return scannerConfiguration.Exporter switch
            {
                FileExporterConfiguration fileExporterConfiguration => new FileExporter(fileExporterConfiguration.Path),
                _ => throw new NotImplementedException("At the moment only file exporter is supported")
            };
        }

        /// <summary>
        /// Gets importer implementation.
        /// </summary>
        /// <returns>At the moment, only file is supported, so returns File Importer object.</returns>
        public IImporter GetImporter()
        {
            var scannerConfiguration = this.configuration.Get();

            return scannerConfiguration.Importer switch
            {
                FileImporterConfiguration fileImporterConfiguration => new FileImporter(fileImporterConfiguration.Path),
                _ => throw new NotImplementedException("At the moment only file importer is supported")
            };
        }

        /// <summary>
        /// Returns a new instance of <see cref="KubernetesImageProvider"/> class.
        /// </summary>
        /// <returns>KubernetesImageProvider instance.</returns>
        public KubernetesImageProvider GetKubeImageProvider()
        {
            var scannerConfiguration = this.configuration.Get();
            return new KubernetesImageProvider(scannerConfiguration.KubeConfigPath);
        }
    }
}