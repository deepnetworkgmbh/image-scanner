using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using core.core;

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
                folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/.kube-scanner/exports";
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            this.folderPath = folderPath;
        }

        public async Task UploadAsync(ScanResult result)
        {
            // write JSON directly to a file
            var img = result
                .ImageName
                .Replace('/', '_')
                .Replace(':', '_');

            var resultPath = Path.Combine(this.folderPath, $"{img}.json");
            await File.WriteAllTextAsync(resultPath, result.ScanResultArray.ToString());

            // write logs
            var logPath = Path.Combine(this.folderPath, $"{img}.log");
            await File.WriteAllTextAsync(logPath, result.Logs);
        }

        public async Task UploadBulkAsync(IEnumerable<ScanResult> results)
        {
            foreach (var r in results)
            {
                // write JSON directly to a file
                var img = r
                    .ImageName
                    .Replace('/', '_')
                    .Replace(':', '_');

                var resultPath = Path.Combine(this.folderPath, $"{img}.json");
                await File.WriteAllTextAsync(resultPath, r.ScanResultArray.ToString());

                // write logs
                var logPath = Path.Combine(this.folderPath, $"{img}.log");
                await File.WriteAllTextAsync(logPath, r.Logs);
            }
        }
    }
}