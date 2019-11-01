using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using kube_scanner.core;
using kube_scanner.helpers;

namespace kube_scanner.scanners
{
    public class Trivy : IScanner
    {
        private const string TrivyImage = "aquasec/trivy:0.1.7";
        private readonly string _cachePath;

        public Trivy(string cachePath)
        {
            if (string.IsNullOrEmpty(cachePath))
            {
                cachePath = (Environment.GetFolderPath(Environment.SpecialFolder.Personal)+"/.kube-scanner/.trivycache");
            }
            
            _cachePath  = cachePath;
        }

        public string ContainerRegistryAddress { get; set; }
        public string ContainerRegistryUserName { get; set; }
        public string ContainerRegistryPassword { get; set; }

        public async Task<ScanResult> Scan(string imageToBeScanned)
        {
            // give a name to container
            var containerName = DockerHelper.CreateRandomContainerName("trivy-container-",8);

            // set the scan result file name
            var scanResultFile = "/" + containerName;

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
            var env = new List<string>();
            if (!string.IsNullOrEmpty(ContainerRegistryAddress))
            {
                var crNameOfImage = imageToBeScanned.Split('/')[0];
                var crNameOfParameter = ContainerRegistryAddress.Split('/')[0];

                if (crNameOfParameter == crNameOfImage)
                {
                    env.AddRange(new[]
                    {
                        "TRIVY_AUTH_URL=" + ContainerRegistryAddress,
                        "TRIVY_USERNAME=" + ContainerRegistryUserName,
                        "TRIVY_PASSWORD=" + ContainerRegistryPassword
                    });
                }
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
            
            foreach(var log in logs.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)) 
            {
                if (!log.Contains("FATAL")) continue;

                var logText = log.Split("FATAL")[1];
                LogHelper.LogErrorsAndContinue(imageToBeScanned, logText);
            }
            
            // create a scan result object
            var trivyScanResult = new ScanResult {ImageName = imageToBeScanned, ScanResultArray = jsonArray, Logs = logs};

            return trivyScanResult;
        }
    }
}