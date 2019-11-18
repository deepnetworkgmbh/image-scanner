using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using core.core;
using Newtonsoft.Json.Linq;
using RunProcessAsTask;
using Serilog;

namespace core.scanners
{
    public class Trivy : IScanner
    {
        private readonly string cachePath;

        public Trivy(string cachePath)
        {
            if (string.IsNullOrEmpty(cachePath))
            {
                cachePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
                            "/.kube-scanner/.trivycache";
            }

            this.cachePath = cachePath;
        }

        public string ContainerRegistryAddress { get; set; }

        public string ContainerRegistryUserName { get; set; }

        public string ContainerRegistryPassword { get; set; }

        public string TrivyBinaryPath { get; set; }

        public async Task<ImageScanDetails> Scan(ContainerImage image)
        {
            // create scan results folder if not already exists
            var scanResultsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                ".kube-scanner",
                "trivy-scan-results");

            if (!Directory.Exists(scanResultsFolder))
            {
                Directory.CreateDirectory(scanResultsFolder);
            }

            // set the scan result file name
            var scanResultFile = scanResultsFolder + CreateRandomFileName("/result-", 6);

            // commands that will be executed by trivy
            var arguments =
                $"--skip-update --cache-dir {this.cachePath} -f json -o {scanResultFile} {image.FullName}";

            // variables to hold logs and json content
            var logs = string.Empty;
            var content = string.Empty;

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = this.TrivyBinaryPath,
                    Arguments = arguments,
                };

                // If the provided private Container Registry (CR) name is equal to CR of image to be scanned,
                // set private CR credentials as env vars to the process
                if (!string.IsNullOrEmpty(this.ContainerRegistryAddress))
                {
                    var crNameOfImage = image.ContainerRegistry;
                    var crNameOfParameter = this.ContainerRegistryAddress.Split('/')[0];

                    if (crNameOfParameter == crNameOfImage)
                    {
                        processStartInfo.EnvironmentVariables["TRIVY_AUTH_URL"] = this.ContainerRegistryAddress;
                        processStartInfo.EnvironmentVariables["TRIVY_USERNAME"] = this.ContainerRegistryUserName;
                        processStartInfo.EnvironmentVariables["TRIVY_PASSWORD"] = this.ContainerRegistryPassword;
                    }
                }

                var logProcessResults = await ProcessEx.RunAsync(processStartInfo);

                content = logProcessResults.ExitCode != 0
                    ? "{}"
                    : JArray.Parse(File.ReadAllText(@scanResultFile)).ToString();

                logs = string.Join(Environment.NewLine, logProcessResults.StandardOutput);
            }
            catch (Exception ex)
            {
                Log.Error("{Text} {Message}", "Error in Trivy process", ex.Message);
            }

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

        private static string CreateRandomFileName(string prefix, int length)
        {
            var random = new Random();

            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

            var str = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return prefix + str;
        }
    }
}