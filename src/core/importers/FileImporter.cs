using System.Collections.Generic;
using System.Threading.Tasks;

using core.core;

namespace core.importers
{
    /// <summary>
    /// Loads Scan Results from files.
    /// </summary>
    public class FileImporter : IImporter
    {
        private readonly string folderPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileImporter"/> class.
        /// </summary>
        /// <param name="folderPath">Path to the folder with Scan Results.</param>
        public FileImporter(string folderPath)
        {
            this.folderPath = folderPath;
        }

        /// <inheritdoc />
        public Task<IEnumerable<ImageScanDetails>> Get(params ContainerImage[] images)
        {
            throw new System.NotImplementedException();
        }
    }
}