using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.core;
using core.helpers;
using Docker.DotNet.Models;
using Serilog;

namespace core.scanners
{
    public class TrivyContainer : IScanner
    {
        private const string TrivyCacheDirectory = "/root/.cache/";
        private const string TrivyImage = "aquasec/trivy:0.1.7";
        private readonly string cachePath;

        public TrivyContainer(string cachePath)
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

        public async Task<ImageScanDetails> Scan(ContainerImage image)
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
                image.FullName,
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
                var crNameOfImage = image.ContainerRegistry;
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

            // get scan result content
            var content = await dockerHelper.GetFileContentFromContainerAsync(scanResultFile);

            // get logs from container
            var logs = await dockerHelper.GetContainerLogsAsync();

            // remove the container
            await dockerHelper.DisposeAsync();

            var result = ImageScanDetails.New();
            result.Image = image;
            result.ScannerType = ScannerType.Trivy;

            var fatalError = logs
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(e => e.Contains("FATAL"));

            if (fatalError != null)
            {
                var logText = fatalError.Split("FATAL")[1];
                Log.Error("{Image} {LogText}", image.FullName, logText);

                result.ScanResult = ScanResult.Failed;
                result.Payload = logText;
            }
            else
            {
                result.ScanResult = ScanResult.Succeeded;
                result.Payload = content;
            }

            return result;
        }
    }
}