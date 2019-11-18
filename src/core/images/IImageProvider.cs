using System.Collections.Generic;
using System.Threading.Tasks;

using core.core;

namespace core.images
{
    /// <summary>
    /// General interface for objects, that provides images to scan.
    /// </summary>
    public interface IImageProvider
    {
        /// <summary>
        /// Returns list of images to scan.
        /// </summary>
        /// <returns>Images to scan.</returns>
        Task<IEnumerable<ContainerImage>> GetImages();
    }
}