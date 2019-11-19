using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using core.core;
using core.helpers;

namespace core.exporters
{
    public class FileExporter : IExporter
    {
        private readonly string folderPath;

        public bool IsBulkUpload { get; set; }

        public FileExporter(string folderPath)
        {
            // if folder path is not provided, use default folder
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    ".kube-scanner",
                    "exports");
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            this.folderPath = folderPath;
        }

        public async Task UploadAsync(ImageScanDetails details)
        {
            // write JSON directly to a file
            var img = details
                .Image
                .FullName
                .Replace('/', '_')
                .Replace(':', '_');

            var resultPath = Path.Combine(this.folderPath, $"{img}.json");
            var jsonResult = JsonSerializerWrapper.Serialize(details);
            await File.WriteAllTextAsync(resultPath, jsonResult);
        }

        public async Task UploadBulkAsync(IEnumerable<ImageScanDetails> results)
        {
            foreach (var result in results)
            {
                // write JSON directly to a file
                var img = result
                    .Image
                    .FullName
                    .Replace('/', '_')
                    .Replace(':', '_');

                var resultPath = Path.Combine(this.folderPath, $"{img}.json");
                var jsonResult = JsonSerializerWrapper.Serialize(result);
                await File.WriteAllTextAsync(resultPath, jsonResult);
            }
        }
    }
}