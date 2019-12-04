using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using core.core;
using core.helpers;

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
            // if folder path is not provided, use default folder
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    ".image-scanner",
                    "exports");
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            this.folderPath = folderPath;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ImageScanDetails>> Get(params ContainerImage[] images)
        {
            var tasks = images.Select(this.DeserializeResult).ToArray();
            await Task.WhenAll(tasks);

            return tasks.Select(i => i.Result);
        }

        private async Task<ImageScanDetails> DeserializeResult(ContainerImage image)
        {
            var safeName = image
                .FullName
                .Replace('/', '_')
                .Replace(':', '_');

            var filePath = Path.Combine(this.folderPath, $"{safeName}.json");

            if (File.Exists(filePath))
            {
                var stringDetails = await File.ReadAllTextAsync(filePath);

                var scanDetails = JsonSerializerWrapper.Deserialize<ImageScanDetails>(stringDetails);
                return scanDetails;
            }
            else
            {
                return ImageScanDetails.NotFound(image);
            }
        }
    }
}