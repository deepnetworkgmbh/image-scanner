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
        private static readonly ILogger Logger = Log.ForContext<Trivy>();
        private static readonly string ScanResultsFolder;

        private readonly string cachePath;
        private readonly string trivyBinaryPath;
        private readonly Dictionary<string, RegistryCredentials> registriesMap;

        static Trivy()
        {
            // create scan results folder if not already exists
            ScanResultsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                ".kube-scanner",
                "trivy-scan-results");

            if (!Directory.Exists(ScanResultsFolder))
            {
                Directory.CreateDirectory(ScanResultsFolder);
            }
        }

        public Trivy(string cachePath, string trivyBinaryPath, RegistryCredentials[] registries)
        {
            if (string.IsNullOrEmpty(cachePath))
            {
                cachePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) +
                            "/.kube-scanner/.trivycache";
            }

            this.cachePath = cachePath;
            this.trivyBinaryPath = trivyBinaryPath;

            this.registriesMap = registries.ToDictionary(i => i.Name);
        }

        public async Task<ImageScanDetails> Scan(ContainerImage image)
        {
            Logger
                .ForContext("Image", image)
                .Information("Scan was started");

            // set the scan result file name
            var scanResultFile = ScanResultsFolder + CreateRandomFileName("/result-", 6);

            // commands that will be executed by trivy
            var arguments =
                $"--skip-update --cache-dir {this.cachePath} -f json -o {scanResultFile} {image.FullName}";

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = this.trivyBinaryPath,
                    Arguments = arguments,
                };

                // If the provided private Container Registry (CR) name is equal to CR of image to be scanned,
                // set private CR credentials as env vars to the process
                if (this.registriesMap.TryGetValue(image.ContainerRegistry, out var registry))
                {
                    Logger
                        .ForContext("Image", image)
                        .Information("Scanning from {RegistryAddress}", registry.Address);

                    processStartInfo.EnvironmentVariables["TRIVY_AUTH_URL"] = registry.Address;
                    processStartInfo.EnvironmentVariables["TRIVY_USERNAME"] = registry.Username;
                    processStartInfo.EnvironmentVariables["TRIVY_PASSWORD"] = registry.Password;
                }
                else
                {
                    Logger
                        .ForContext("Image", image)
                        .Information("Scanning from {RegistryAddress}", "Default Docker Hub");
                }

                var processResults = await ProcessEx.RunAsync(processStartInfo);

                var scanOutput = processResults.ExitCode != 0
                    ? "{}"
                    : JArray.Parse(File.ReadAllText(@scanResultFile)).ToString();

                Logger
                    .ForContext("Image", image)
                    .Information("Scan was finished with exit code {ExitCode} in {ScanningTime}", processResults.ExitCode, processResults.RunTime);

                var logs = string.Join(Environment.NewLine, processResults.StandardOutput);

                var result = ImageScanDetails.New();
                result.Image = image;
                result.ScannerType = ScannerType.Trivy;

                var fatalError = logs
                    .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault(e => e.Contains("FATAL"));

                if (fatalError != null)
                {
                    var fatalLogText = fatalError.Split("FATAL")[1];

                    Logger
                        .ForContext("Image", image)
                        .Error("Scan failed: {FailedScanLogs}", fatalLogText);

                    result.ScanResult = ScanResult.Failed;
                    result.Payload = fatalLogText;
                }
                else
                {
                    Logger
                        .ForContext("Image", image)
                        .Error("Scan succeeded");

                    result.ScanResult = ScanResult.Succeeded;
                    result.Payload = scanOutput;
                }

                return result;
            }
            catch (Exception ex)
            {
                Log
                    .ForContext("Image", image)
                    .Error(ex, "Error in Trivy process");

                var result = ImageScanDetails.New();
                result.Image = image;
                result.ScannerType = ScannerType.Trivy;
                result.ScanResult = ScanResult.Failed;
                result.Payload = ex.Message;

                return result;
            }
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