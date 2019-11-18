using System.Threading.Tasks;

using core;
using core.images;

using Microsoft.AspNetCore.Mvc;

using webapp.Configuration;

namespace webapp.Controllers
{
    /// <summary>
    /// Entry point to trigger new image scans.
    /// </summary>
    [ApiController]
    [Route("scan")]
    public class ScanController : ControllerBase
    {
        private readonly KubeScannerConfiguration configuration;
        private readonly KubernetesImageProvider k8SImages;
        private readonly KubeScannerFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanController"/> class.
        /// </summary>
        public ScanController(ConfigurationParser configuration, KubernetesImageProvider k8sImages, KubeScannerFactory factory)
        {
            this.configuration = configuration.Get();
            this.k8SImages = k8sImages;
            this.factory = factory;
        }

        /// <summary>
        /// Scans all the visible images in Kubernetes cluster.
        /// </summary>
        /// <returns>Acknowledgement.</returns>
        [HttpPost]
        [Route("k8s")]
        public async Task ScanKubernetes()
        {
            await KubeScanner.Scan(this.factory.GetScanner(), this.factory.GetExporter(), this.k8SImages, this.configuration.Parallelization);
        }

        /// <summary>
        /// Scan all the images specified in a request body.
        /// </summary>
        /// <param name="images">Array of image tags to scan.</param>
        /// <returns>Acknowledgement.</returns>
        [HttpPost]
        [Route("images")]
        public async Task ScanImages([FromBody]string[] images)
        {
            var imageProvider = new InMemoryImageProvider(images);
            await KubeScanner.Scan(this.factory.GetScanner(), this.factory.GetExporter(), imageProvider, this.configuration.Parallelization);
        }
    }
}
