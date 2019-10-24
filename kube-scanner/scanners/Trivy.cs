using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using kube_scanner.core;
using kube_scanner.helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace kube_scanner.scanners
{
    public class Trivy : IScanner
    {
        private  const string TrivyImage = "aquasec/trivy:latest";
       
        public Trivy()
        {

        }

        public async Task<ScanResult> Scan(string imageToBeScanned)
        {
            // give a name to container
            var containerName = "trivy-container-"+(new Random().Next(10, 10000));
            
            var outputFile = "/trivy_scan_results/"+containerName;
            
            var cmd = new[]
            {
                "-c",
                "-f", "json",
                "-o", outputFile,
                imageToBeScanned
            };

            var folderPath = Directory.GetCurrentDirectory();
            
            var hostConfig = new HostConfig
            {
                Binds = new List<string>
                {
                    "/var/run/docker.sock:/var/run/docker.sock",
                    folderPath+"/Library/Caches:/root/.cache/",
                    folderPath+"/trivy_scan_results:/trivy_scan_results"
                }
            };
            
            // create docker helper
            var dockerHelper = new DockerHelper(TrivyImage, containerName, cmd, hostConfig);
            
            // start to scan the image
            await dockerHelper.StartContainer(imageToBeScanned);

            // remove the container
            await dockerHelper.DisposeAsync();

            JArray jsonArray;
            
            try
            {
                jsonArray = JArray.Parse(File.ReadAllText(@folderPath+outputFile));
            }
            catch (JsonReaderException e)
            {
                Console.WriteLine("FATAL error in image scan: failed to scan image {0}: failed to analyze OS: Unknown OS", imageToBeScanned);
                jsonArray = new JArray(); // empty array for not scanned images 
            }
            
            var trivyScanResult = new ScanResult {ImageName = imageToBeScanned, ScanResultArray = jsonArray};

            return trivyScanResult;
        }
    }
}