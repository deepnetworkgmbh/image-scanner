using System;
using System.Collections.Generic;
using System.IO;
using kube_scanner.core;

namespace kube_scanner.exporters
{
    public class FileExporter : IExporter
    {
        private readonly string _folderPath;

        public FileExporter(string folderPath)
        {
            var fp = $"{folderPath}/files/";

            if (!Directory.Exists(fp))
                Directory.CreateDirectory(fp);
            
            _folderPath = fp;
        }

        public void Upload(ScanResult result)
        {
            // write JSON directly to a file
            var img = result.ImageName.Replace('/', '_');
            
            File.WriteAllText(@_folderPath+img+".json", result.ScanResultArray.ToString());
        }

        public void UploadBulk(IEnumerable<ScanResult> results)
        {
            foreach (var r in results)
            {
                // write JSON directly to a file
                var img = r.ImageName.Replace('/', '_');
            
                File.WriteAllText(@_folderPath+img+".json", r.ScanResultArray.ToString());    
            }
        }
    }
}