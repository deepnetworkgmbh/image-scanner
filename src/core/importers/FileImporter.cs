using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using core.core;
using core.helpers;

using Serilog;

namespace core.importers
{
    /// <summary>
    /// Loads Scan Results from files.
    /// </summary>
    public class FileImporter : IImporter
    {
        private static readonly ILogger Logger = Log.ForContext<FileImporter>();

        private readonly string folderPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileImporter"/> class.
        /// </summary>
        /// <param name="folderPath">Path to the folder with Scan Results.</param>
        public FileImporter(string folderPath)
        {
            // if folder path is not provided, use default folder
            if (string.IsNullOrEmpty(folderPath))
            {
                folderPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                    ".image-scanner",
                    "exports");
            }

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            this.folderPath = folderPath;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<ImageScanDetails>> Get(params ContainerImage[] images)
        {
            var tasks = images.Select(this.DeserializeResult).ToArray();
            await Task.WhenAll(tasks);

            return tasks.Select(i => i.Result);
        }

        /// <inheritdoc />
        public async Task<CveDetails> GetCve(string id)
        {
            Logger.Information("Getting CVE details by {CveId}", id);

            try
            {
                var files = Directory.GetFiles(this.folderPath);

                // if files amount is more than 100 - batch them
                const int batchSize = 100;
                var details = new CveDetails[(files.Length / batchSize) + 1];

                for (var i = 0; i < files.Length; i += batchSize)
                {
                    var filesLeft = files.Length - i;
                    var nextBatchSize = filesLeft < batchSize ? filesLeft : batchSize;

                    Logger.Information(
                        "Calculating summary for files {StartIndex}-{EndIndex} of {TotalFiles}",
                        i,
                        nextBatchSize,
                        files.Length);

                    var batchToProcess = files
                        .Skip(i)
                        .Take(nextBatchSize)
                        .ToArray();
                    details[i / batchSize] = await GetCveDetailsFromFiles(id, batchToProcess);
                }

                return SummarizeCveDetails(details);
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to get CVE details for {CveId}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<CveDetails[]> CveOverview()
        {
            Logger.Information("Getting CVE summary");

            try
            {
                var files = Directory.GetFiles(this.folderPath);

                // if files amount is more than 100 - batch them
                const int batchSize = 100;
                var cveDict = new ConcurrentDictionary<string, CveDetails>();

                for (var i = 0; i < files.Length; i += batchSize)
                {
                    var filesLeft = files.Length - i;
                    var nextBatchSize = filesLeft < batchSize ? filesLeft : batchSize;

                    Logger.Information(
                        "Calculating summary for files {StartIndex}-{EndIndex} of {TotalFiles}",
                        i,
                        nextBatchSize,
                        files.Length);

                    var batchToProcess = files
                        .Skip(i)
                        .Take(nextBatchSize)
                        .ToArray();
                    await CveBatchOverview(batchToProcess, cveDict);
                }

                return cveDict.Values.Select(v => v.DeduplicateImages()).ToArray();
            }
            catch (Exception ex)
            {
                Logger.Warning(ex, "Failed to get CVE summary");
                throw;
            }
        }

        private static async Task CveBatchOverview(string[] files, ConcurrentDictionary<string, CveDetails> cveDict)
        {
            // Reads files content in parallel
            var readers = files
                .Select(async f =>
                {
                    var content = await File.ReadAllTextAsync(f);
                    OverviewSingleFile(content, cveDict);
                })
                .ToArray();
            await Task.WhenAll(readers);
        }

        private static void OverviewSingleFile(string scanContent, ConcurrentDictionary<string, CveDetails> cveDict)
        {
            // Deserialize envelope object
            var scanDetails = JsonSerializerWrapper.Deserialize<ImageScanDetails>(scanContent);

            // If scan result failed - nothing to do here
            if (scanDetails.ScanResult == ScanResult.Succeeded)
            {
                var targets = JsonSerializerWrapper.Deserialize<TrivyScanTarget[]>(scanDetails.Payload);

                // Tries to find CVE with the same id
                foreach (var trivyResult in targets.Where(t => t.Vulnerabilities != null).SelectMany(t => t.Vulnerabilities))
                {
                    cveDict.AddOrUpdate(
                        trivyResult.VulnerabilityID,
                        k => CveDetails.FromTrivyDescription(trivyResult, scanDetails.Image),
                        (k, v) =>
                        {
                            v.ImageTags.Add(scanDetails.Image.FullName);
                            return v;
                        });
                }
            }
        }

        private static async Task<CveDetails> GetCveDetailsFromFiles(string id, string[] files)
        {
            // Reads files content in parallel and converts to CveDetails objects
            var tasks = files
                .Select(async f =>
                {
                    var content = await File.ReadAllTextAsync(f);
                    return ConvertToCve(content, id);
                })
                .ToArray();
            await Task.WhenAll(tasks);

            // Reduces the resulting object from the batch.
            var cveDetails = SummarizeCveDetails(tasks.Select(task => task.Result).ToArray());

            return cveDetails;
        }

        private static CveDetails SummarizeCveDetails(CveDetails[] cveItems)
        {
            // Gets a list of unique image tags
            var uniqueImages = cveItems
                .Where(i => i != null)
                .SelectMany(i => i.ImageTags)
                .Where(i => i != null)
                .Distinct()
                .ToList();

            // All details are _expected_ to be the same, so let's take the first one
            var cveDetails = cveItems.FirstOrDefault(cve => cve != null);
            if (cveDetails != null)
            {
                cveDetails.ImageTags = uniqueImages;
            }

            return cveDetails;
        }

        private static CveDetails ConvertToCve(string scanContent, string cveId)
        {
            // Deserialize envelope object
            var scanDetails = JsonSerializerWrapper.Deserialize<ImageScanDetails>(scanContent);

            // If scan result failed - nothing to do here
            if (scanDetails.ScanResult == ScanResult.Succeeded)
            {
                var targets = JsonSerializerWrapper.Deserialize<TrivyScanTarget[]>(scanDetails.Payload);

                // Tries to find CVE with the same id
                var cve = targets
                    .Where(t => t.Vulnerabilities != null)
                    .SelectMany(t => t.Vulnerabilities)
                    .FirstOrDefault(vd => vd.VulnerabilityID == cveId);

                if (cve != null)
                {
                    return CveDetails.FromTrivyDescription(cve, scanDetails.Image);
                }
            }

            return null;
        }

        private async Task<ImageScanDetails> DeserializeResult(ContainerImage image)
        {
            var safeName = image
                .FullName
                .Replace('/', '_')
                .Replace(':', '_');

            var filePath = Path.Combine(this.folderPath, $"{safeName}.json");

            if (File.Exists(filePath))
            {
                var stringDetails = await File.ReadAllTextAsync(filePath);

                var scanDetails = JsonSerializerWrapper.Deserialize<ImageScanDetails>(stringDetails);
                return scanDetails;
            }
            else
            {
                return ImageScanDetails.NotFound(image);
            }
        }
    }
}