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
    }
}