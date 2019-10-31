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
            var img = result.ImageName.Replace('/', '_');
            
            File.WriteAllText(@_folderPath+"/"+img+".json", result.ScanResultArray.ToString());
            
            // write logs
            File.WriteAllText(@_folderPath+"/"+img+".log", result.Logs);
        }

        public void UploadBulk(IEnumerable<ScanResult> results)
        {
            foreach (var r in results)
            {
                // write JSON directly to a file
                var img = r.ImageName.Replace('/', '_');
            
                File.WriteAllText(@_folderPath+"/"+img+".json", r.ScanResultArray.ToString());
                
                // write logs
                File.WriteAllText(@_folderPath+"/"+img+".log", r.Logs);
            }
        }
    }
}