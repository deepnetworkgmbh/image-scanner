using System.Threading.Tasks;

using core;
using core.images;

using Microsoft.AspNetCore.Mvc;

namespace webapp.Controllers
{
    /// <summary>
    /// Entry point to trigger new image scans.
    /// </summary>
    [ApiController]
    [Route("scan")]
    public class ScanController : ControllerBase
    {
        private readonly KubeScanner scanner;
        private readonly KubernetesImageProvider imageProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanController"/> class.
        /// </summary>
        public ScanController(KubeScanner scanner, KubernetesImageProvider imageProvider)
        {
            this.scanner = scanner;
            this.imageProvider = imageProvider;
        }

        /// <summary>
        /// Scans all the visible images in Kubernetes cluster.
        /// </summary>
        /// <returns>Acknowledgement.</returns>
        [HttpPost]
        [Route("k8s")]
        public async Task<StatusCodeResult> ScanKubernetes()
        {
            await this.scanner.Scan(this.imageProvider);
            return this.StatusCode(201);
        }

        /// <summary>
        /// Scan all the images specified in a request body.
        /// </summary>
        /// <param name="images">Array of image tags to scan.</param>
        /// <returns>Acknowledgement.</returns>
        [HttpPost]
        [Route("images")]
        public async Task<StatusCodeResult> ScanImages([FromBody]string[] images)
        {
            var inMemoryProvider = new InMemoryImageProvider(images);
            await this.scanner.Scan(inMemoryProvider);
            return this.StatusCode(201);
        }
    }
}
