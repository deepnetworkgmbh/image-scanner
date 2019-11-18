using System.Threading.Tasks;

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
        /// <summary>
        /// Returns scan results for requested images.
        /// </summary>
        /// <param name="images">Array of image tags.</param>
        /// <returns>Scan results.</returns>
        [HttpGet]
        [Route("")]
        public async Task ScanImages([FromQuery]string[] images)
        {
            await Task.Yield();
        }
    }
}
