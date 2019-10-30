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
        private const string TrivyImage = "aquasec/trivy:latest";
        private readonly string _cachePath;
        
        public string ContainerRegistryAddress { get; set; }
        public string ContainerRegistryUserName { get; set; }
        public string ContainerRegistryPassword { get; set; }
        
        public Trivy(string cachePath)
        {
            _cachePath  = cachePath;
        }

        public async Task<ScanResult> Scan(string imageToBeScanned)
        {
            // give a name to container
            var containerName = "trivy-container-"+(new Random().Next(10, 10000));
         
            // set the scan result file name
            var scanResultFile = "/"+containerName;
            
            // commands that will be executed by trivy container
            var cmd = new[]
            {
                "-c",
                "-f", "json",
                "-o", scanResultFile,
                imageToBeScanned
            };
            
            // set the volumes will be mounted to trivy container
            var hostConfig = new HostConfig
            {
                Binds = new List<string>
                {
                    _cachePath  + ":/root/.cache/"
                }
            };

            // If the provided private Container Registry (CR) name is equal to CR of image to be scanned,
            // add private CR credentials to the trivy container as env vars
            var crNameOfImage = imageToBeScanned.Split('/')[0];
            var crNameOfParameter = ContainerRegistryAddress.Split('/')[0];
            var env = new List<string>();
            if (crNameOfParameter == crNameOfImage)
            {
                env.AddRange(new string[]
                {
                    "TRIVY_AUTH_URL="+ContainerRegistryAddress, 
                    "TRIVY_USERNAME="+ContainerRegistryUserName,
                    "TRIVY_PASSWORD="+ContainerRegistryPassword             
                });
            }

            // create docker helper
            var dockerHelper = new DockerHelper(TrivyImage, containerName, cmd, hostConfig, env);
            
            // start to scan the image
            await dockerHelper.StartContainer();

            // get json array from container
            var jsonArray = await dockerHelper.GetArchiveFromContainerAsync(scanResultFile);
            
            // get logs from container
            var logs = await dockerHelper.GetContainerLogsAsync();

            // remove the container
            await dockerHelper.DisposeAsync();
            
            foreach(var log in logs.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) 
            {
                if (!log.Contains("FATAL")) continue;
                
                Console.WriteLine("SCAN ERROR on Image {0}", imageToBeScanned);
                Console.WriteLine(log);
            }
            
            // create a scan result object
            var trivyScanResult = new ScanResult {ImageName = imageToBeScanned, ScanResultArray = jsonArray, Logs = logs};

            return trivyScanResult;
        }
    }
}