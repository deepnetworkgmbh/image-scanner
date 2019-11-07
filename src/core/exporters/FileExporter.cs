using System;
using System.Collections.Generic;
using System.IO;
using core.core;

namespace core.exporters
{
    public class FileExporter : IExporter
    {
        private readonly string _folderPath;
        
        public bool IsBulkUpload { get; set; }

        public FileExporter(string folderPath)
        {
            // if folder path is not provided, use default folder
            if (string.IsNullOrEmpty(folderPath))
                folderPath = (Environment.GetFolderPath(Environment.SpecialFolder.Personal)+"/.kube-scanner/exports");
            
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);
            
            _folderPath = folderPath;
        }

        public void Upload(ScanResult result)
        {
            // write JSON directly to a file
            var img = result
                .ImageName
                .Replace('/', '_')
                .Replace(':', '_');

            var resultPath = Path.Combine(_folderPath, $"{img}.json");
            File.WriteAllText(resultPath, result.ScanResultArray.ToString());

            // write logs
            var logPath = Path.Combine(_folderPath, $"{img}.log");
            File.WriteAllText(logPath, result.Logs);
        }

        public void UploadBulk(IEnumerable<ScanResult> results)
        {
            foreach (var r in results)
            {
                // write JSON directly to a file
                var img = r
                    .ImageName
                    .Replace('/', '_')
                    .Replace(':', '_');

                var resultPath = Path.Combine(_folderPath, $"{img}.json");
                File.WriteAllText(resultPath, r.ScanResultArray.ToString());

                // write logs
                var logPath = Path.Combine(_folderPath, $"{img}.log");
                File.WriteAllText(logPath, r.Logs);
            }
        }
    }
}