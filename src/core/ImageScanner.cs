using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using core.core;
using core.exporters;
using core.images;
using core.importers;
using core.scanners;
using Serilog;

namespace core
{
    public class ImageScanner : IDisposable, IAsyncDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<ImageScanner>();

        private readonly IImporter importer;

        private readonly TransformBlock<ContainerImage, ImageScanDetails> scannerBlock;
        private readonly ActionBlock<ImageScanDetails> exporterBlock;

        private readonly Timer logWriter;

        public ImageScanner(IScanner scanner, IExporter exporter, IImporter importer, int parallelismDegree, int bufferSize)
        {
            this.importer = importer;

            // create the pipeline of actions
            this.scannerBlock = new TransformBlock<ContainerImage, ImageScanDetails>(
                scanner.Scan,
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = bufferSize,
                    MaxDegreeOfParallelism = parallelismDegree,
                    EnsureOrdered = false,
                });

            this.exporterBlock = new ActionBlock<ImageScanDetails>(
                exporter.UploadAsync,
                new ExecutionDataflowBlockOptions
                {
                    BoundedCapacity = bufferSize,
                    MaxDegreeOfParallelism = parallelismDegree,
                    EnsureOrdered = false,
                });

            // link the actions
            this.scannerBlock.LinkTo(
                this.exporterBlock,
                new DataflowLinkOptions
                {
                    PropagateCompletion = true,
                });

            this.logWriter = new Timer(this.LogBlocksStats, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public async Task<int> Scan(IImageProvider images)
        {
            try
            {
                var containerImages = (await images.GetImages()).ToArray();
                var imageScanDetails =
                    (await this.importer.Get(containerImages))
                    .Where(i => i.ScanResult != ScanResult.NotFound)
                    .ToDictionary(i => i.Image);

                var now = DateTime.UtcNow;
                var imagesToScan = containerImages
                    .Where(img => !imageScanDetails.TryGetValue(img, out var details) || (now - details.Timestamp).Hours > 12)
                    .ToArray();

                Logger.Information(
                    "{UpToDateImages} scan results are up to date. Scanning {ImagesToScan}",
                    containerImages.Length - imagesToScan.Length,
                    imagesToScan.Length);

                foreach (var image in imagesToScan)
                {
                    await this.scannerBlock.SendAsync(image);
                }

                this.logWriter.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

                Logger.Information("Finished enqueueing {ImageCount} images", imagesToScan.Length);
                return imagesToScan.Length;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to scan images");
                throw;
            }
        }

        public async Task Complete()
        {
            Logger.Information("Completing ImageScanner");

            this.scannerBlock.Complete();

            await this.exporterBlock.Completion;

            await this.logWriter.DisposeAsync();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Complete().Wait();
        }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            return new ValueTask(this.Complete());
        }

        private void LogBlocksStats(object state)
        {
            Logger.Information(
                "Scanner has {InputCount} messages in inbox and {OutputCount} messages in outbox",
                this.scannerBlock.InputCount,
                this.scannerBlock.OutputCount);
            Logger.Information("Exporter has {InputCount} messages in inbox", this.exporterBlock.InputCount);

            if (this.scannerBlock.InputCount == 0 &&
                this.scannerBlock.OutputCount == 0 &&
                this.exporterBlock.InputCount == 0)
            {
                this.logWriter.Change(TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(30));
            }
        }
    }
}
