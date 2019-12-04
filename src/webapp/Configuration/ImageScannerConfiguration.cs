namespace webapp.Configuration
{
    /// <summary>
    /// The application configuration.
    /// </summary>
    public class ImageScannerConfiguration
    {
        /// <summary>
        /// The paralellization degree.
        /// </summary>
        public int Parallelization { get; set; }

        /// <summary>
        /// The buffer size.
        /// </summary>
        public int Buffer { get; set; }

        /// <summary>
        /// The Kubernetes related options.
        /// </summary>
        public KubernetesConfiguration Kube { get; set; }

        /// <summary>
        /// The scanner configuration.
        /// </summary>
        public IScannerConfiguration Scanner { get; set; }

        /// <summary>
        /// The Scan Result exporter configuration.
        /// </summary>
        public IExporterConfiguration Exporter { get; set; }

        /// <summary>
        /// The Scan Result importer configuration.
        /// </summary>
        public IImporterConfiguration Importer { get; set; }
    }

    /// <summary>
    /// The base interface for all types of scanners configuration.
    /// </summary>
    public interface IScannerConfiguration
    {
    }

    /// <summary>
    /// The base interface for all types of exporters configuration.
    /// </summary>
    public interface IExporterConfiguration
    {
    }

    /// <summary>
    /// The base interface for all types of importers configuration.
    /// </summary>
    public interface IImporterConfiguration
    {
    }
}