using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using core.core;

using Microsoft.AspNetCore.Mvc;

namespace webapp.Controllers
{
    /// <summary>
    /// Provides scan results.
    /// </summary>
    [ApiController]
    [Route("scan-results")]
    public class ScanResultsController : ControllerBase
    {
        private readonly KubeScannerFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanResultsController"/> class.
        /// </summary>
        public ScanResultsController(KubeScannerFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Returns scan results for requested images.
        /// </summary>
        /// <param name="images">Array of image tags.</param>
        /// <returns>Scan results.</returns>
        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<ImageScanDetails>> ScanImages([FromQuery]string[] images)
        {
            // TODO: try to scan images in real-time?
            var containerImages = images.Select(ContainerImage.FromFullName).ToArray();

            var results = await this.factory.GetImporter().Get(containerImages);

            return results;
        }
    }
}
