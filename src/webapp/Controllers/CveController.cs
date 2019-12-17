using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace webapp.Controllers
{
    /// <summary>
    /// Provides scan results.
    /// </summary>
    [ApiController]
    [Route("cve")]
    public class CveController : ControllerBase
    {
        private readonly ImageScannerFactory factory;

        /// <summary>
        /// Initializes a new instance of the <see cref="CveController"/> class.
        /// </summary>
        public CveController(ImageScannerFactory factory)
        {
            this.factory = factory;
        }

        /// <summary>
        /// Returns detailed CVE description and images, where it was used.
        /// </summary>
        /// <param name="id">The CVE identifier.</param>
        /// <returns>CVE details.</returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<ObjectResult> GetCveDetails([FromRoute]string id)
        {
            try
            {
                var cve = await this.factory.GetImporter().GetCve(id);

                if (cve == null)
                {
                    return this.NotFound(id);
                }

                return this.Ok(cve);
            }
            catch (Exception)
            {
                return this.StatusCode(500, null);
            }
        }

        /// <summary>
        /// Returns CVEs overview.
        /// </summary>
        [HttpGet]
        [Route("overview")]
        public async Task<ObjectResult> GetCveDetails()
        {
            try
            {
                var cveDetails = await this.factory.GetImporter().CveOverview();
                return this.Ok(cveDetails);
            }
            catch (Exception)
            {
                return this.StatusCode(500, null);
            }
        }
    }
}
