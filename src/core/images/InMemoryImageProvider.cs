using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using core.core;

namespace core.images
{
    /// <summary>
    /// Stores list of images in memory and returns them on request.
    /// </summary>
    public class InMemoryImageProvider : IImageProvider
    {
        private readonly ContainerImage[] images;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryImageProvider"/> class.
        /// </summary>
        /// <param name="tags">List of image full tags.</param>
        public InMemoryImageProvider(IEnumerable<string> tags)
        {
            this.images = tags.Select(ContainerImage.FromFullName).ToArray();
        }

        /// <inheritdoc />
        public Task<IEnumerable<ContainerImage>> GetImages()
        {
            return Task.FromResult<IEnumerable<ContainerImage>>(this.images);
        }
    }
}