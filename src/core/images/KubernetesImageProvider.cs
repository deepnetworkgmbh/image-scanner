using System.Collections.Generic;
using System.Threading.Tasks;

using core.core;

using Serilog;

namespace core.images
{
    public class KubernetesImageProvider : IImageProvider
    {
        private static readonly ILogger Logger = Log.ForContext<KubernetesImageProvider>();

        private readonly string[] kubeNamespaces;
        private readonly SharpCompress.Lazy<Task<KubeClient>> kubeClientTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="KubernetesImageProvider"/> class.
        /// </summary>
        /// <param name="kubeConfigPath">Path to kube-config file.</param>
        /// <param name="kubeNamespaces">List of namespaces to query images from.</param>
        public KubernetesImageProvider(string kubeConfigPath, params string[] kubeNamespaces)
        {
            this.kubeNamespaces = kubeNamespaces;
            this.kubeClientTask = new SharpCompress.Lazy<Task<KubeClient>>(() => InitKubeClient(kubeConfigPath));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ContainerImage>> GetImages()
        {
            // create a Kubernetes client (factory pattern)
            var kubeClient = await this.kubeClientTask.Value;

            // retrieve the unique list of images in the cluster
            var images = await kubeClient.GetImages(this.kubeNamespaces);

            return images;
        }

        private static async Task<KubeClient> InitKubeClient(string kubeConfigPath)
        {
            Logger.Information("Initializing Kube client");
            return await KubeClient.CreateAsync(kubeConfigPath);
        }
    }
}