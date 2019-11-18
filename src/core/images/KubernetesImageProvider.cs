using System.Collections.Generic;
using System.Threading.Tasks;

using core.core;

using SharpCompress;

namespace core.images
{
    public class KubernetesImageProvider : IImageProvider
    {
        private readonly Lazy<Task<KubeClient>> kubeClientTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="KubernetesImageProvider"/> class.
        /// </summary>
        /// <param name="kubeConfigPath">Path to kube-config file.</param>
        public KubernetesImageProvider(string kubeConfigPath)
        {
            this.kubeClientTask = new Lazy<Task<KubeClient>>(() => KubeClient.CreateAsync(kubeConfigPath));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ContainerImage>> GetImages()
        {
            // create a Kubernetes client (factory pattern)
            var kubeClient = await this.kubeClientTask.Value;

            // retrieve the unique list of images in the cluster
            var images = await kubeClient.GetImages();

            return images;
        }
    }
}