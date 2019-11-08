using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.core;
using core.helpers;
using Docker.DotNet.Models;
using Serilog;

namespace core.scanners
{
    public class Trivy : IScanner
    {
        private const string TrivyCacheDirectory = "/root/.cache/";
        private const string TrivyImage = "aquasec/trivy:0.1.7";
        private readonly string cachePath;

        public Trivy(string cachePath)
        {
            if (string.IsNullOrEmpty(cachePath))
            {
                cachePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "/.kube-scanner/.trivycache";
            }

            this.cachePath = cachePath;
        }

        public string ContainerRegistryAddress { get; set; }

        public string ContainerRegistryUserName { get; set; }

        public string ContainerRegistryPassword { get; set; }

        public async Task<ScanResult> Scan(string imageToBeScanned)
        {
            // create docker helper
            var dockerHelper = new DockerContainer
            {
                ContainerImage = TrivyImage,
            };

            // give a name to container
            var containerName = DockerContainer.CreateRandomContainerName("trivy-container-", 8);

            // set the scan result file name
            var scanResultFile = "/" + containerName;

            // commands that will be executed by trivy container
            var cmd = new[]
            {
                "--skip-update",
                "--cache-dir", TrivyCacheDirectory,
                "-f", "json",
                "-o", scanResultFile,
                imageToBeScanned,
            };

            // set the bind to be mounted to trivy container
            var hostConfig = new HostConfig
            {
                Mounts = new List<Mount>
                {
                    new Mount
                    {
                        Source = this.cachePath,
                        Target = TrivyCacheDirectory,
                        Type = "bind",
                    },
                },
            };

            // If the provided private Container Registry (CR) name is equal to CR of image to be scanned,
            // add private CR credentials to the trivy container as env vars
            var env = new List<string>();
            if (!string.IsNullOrEmpty(this.ContainerRegistryAddress))
            {
                var crNameOfImage = imageToBeScanned.Split('/')[0];
                var crNameOfParameter = this.ContainerRegistryAddress.Split('/')[0];

                if (crNameOfParameter == crNameOfImage)
                {
                    env.AddRange(new[]
                    {
                        "TRIVY_AUTH_URL=" + this.ContainerRegistryAddress,
                        "TRIVY_USERNAME=" + this.ContainerRegistryUserName,
                        "TRIVY_PASSWORD=" + this.ContainerRegistryPassword,
                    });
                }
            }

            // set docker container properties
            dockerHelper.ContainerName = containerName;
            dockerHelper.Cmd = cmd;
            dockerHelper.HostConfig = hostConfig;
            dockerHelper.Env = env;

            // pull Trivy image
            await dockerHelper.PullImage();

            // start to scan the image
            await dockerHelper.StartContainer();

            // get json array from container
            var jsonArray = await dockerHelper.GetArchiveFromContainerAsync(scanResultFile);

            // get logs from container
            var logs = await dockerHelper.GetContainerLogsAsync();

            // remove the container
            await dockerHelper.DisposeAsync();

            foreach (var log in logs.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!log.Contains("FATAL"))
                {
                    continue;
                }

                var logText = log.Split("FATAL")[1];

                // LogHelper.LogErrorsAndContinue(imageToBeScanned, logText);
                Log.Error("{Image} {LogText}", imageToBeScanned, logText);
            }

            // create a scan result object
            var trivyScanResult = new ScanResult { ImageName = imageToBeScanned, ScanResultArray = jsonArray, Logs = logs };

            return trivyScanResult;
        }
    }
}