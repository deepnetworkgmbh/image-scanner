using System.Threading.Tasks;

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
        /// <summary>
        /// Scans all the visible images in Kubernetes cluster.
        /// </summary>
        /// <returns>Acknowledgement.</returns>
        [HttpPost]
        [Route("k8s")]
        public async Task ScanKubernetes()
        {
            await Task.Yield();
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
            await Task.Yield();
        }
    }
}
