namespace webapp.Configuration
{
    /// <summary>
    /// Represents File Importer configuration.
    /// </summary>
    public class FileImporterConfiguration : IImporterConfiguration
    {
        /// <summary>
        /// Path to the folder with Scan Results.
        /// </summary>
        public string Path { get; set; }
    }
}