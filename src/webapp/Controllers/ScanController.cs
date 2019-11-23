using System;
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
        public async Task<ObjectResult> ScanKubernetes()
        {
            try
            {
                var count = await this.scanner.Scan(this.imageProvider);
                return this.StatusCode(201, $"Enqueued {count} images to scan");
            }
            catch (Exception ex)
            {
                return this.StatusCode(500, $"Scan process failed due {ex.Message}");
            }
        }

        /// <summary>
        /// Scan all the images specified in a request body.
        /// </summary>
        /// <param name="images">Array of image tags to scan.</param>
        /// <returns>Acknowledgement.</returns>
        [HttpPost]
        [Route("images")]
        public async Task<ObjectResult> ScanImages([FromBody] string[] images)
        {
            try
            {
                var inMemoryProvider = new InMemoryImageProvider(images);
                var count = await this.scanner.Scan(inMemoryProvider);
                return this.StatusCode(201, $"Enqueued {count} images to scan");
            }
            catch (Exception ex)
            {
                return this.StatusCode(500, $"Scan process failed due {ex.Message}");
            }
        }
    }
}
