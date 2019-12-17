using System.Collections.Generic;
using System.Threading.Tasks;

using core.core;

namespace core.importers
{
    /// <summary>
    /// Loads Scan Results.
    /// </summary>
    public interface IImporter
    {
        /// <summary>
        /// Returns scan result for requested images.
        /// </summary>
        /// <param name="images">List of images to query results.</param>
        /// <returns>Scan results.</returns>
        Task<IEnumerable<ImageScanDetails>> Get(params ContainerImage[] images);

        /// <summary>
        /// Returns detailed CVE information and images, where it was used.
        /// </summary>
        /// <param name="id">Unique CVE identifier.</param>
        /// <returns>CVE details.</returns>
        Task<CveDetails> GetCve(string id);

        /// <summary>
        /// Summarizes what CVEs where found.
        /// </summary>
        /// <returns>List of all found CVEs.</returns>
        Task<CveDetails[]> CveOverview();
    }
}