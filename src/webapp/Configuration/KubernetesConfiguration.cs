namespace webapp.Configuration
{
    /// <summary>
    /// Represents Kubernetes configuration.
    /// </summary>
    public class KubernetesConfiguration
    {
        /// <summary>
        /// Path to kube config.
        /// </summary>
        public string ConfigPath { get; set; }

        /// <summary>
        /// Array of namespaces to get images from.
        /// </summary>
        public string[] Namespaces { get; set; }
    }
}