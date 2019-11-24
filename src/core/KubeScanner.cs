using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using core.core;
using core.exporters;
using core.images;
using core.scanners;
using Serilog;

namespace core
{
    public class KubeScanner : IDisposable, IAsyncDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<KubeScanner>();

        private readonly TransformBlock<ContainerImage, ImageScanDetails> scannerBlock;
        private readonly ActionBlock<ImageScanDetails> exporterBlock;

        private readonly Timer logWriter;

        public KubeScanner(IScanner scanner, IExporter exporter, int parallelismDegree, int bufferSize)
        {
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
            var counter = 0;
            try
            {
                var containerImages = (await images.GetImages()).ToArray();

                Logger.Information("Scanning {ImageCount} images", containerImages.Length);

                foreach (var image in containerImages)
                {
                    await this.scannerBlock.SendAsync(image);
                    counter++;
                }

                this.logWriter.Change(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

                Logger.Information("Finished enqueueing {ImageCount} images", containerImages.Length);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to scan images");
                throw;
            }

            return counter;
        }

        public async Task Complete()
        {
            Logger.Information("Completing KubeScanner");

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
