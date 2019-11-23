using System;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using core.core;
using core.exporters;
using core.images;
using core.scanners;
using Serilog;

namespace core
{
    public class KubeScanner : IDisposable
    {
        private static readonly ILogger Logger = Log.ForContext<KubeScanner>();

        private readonly TransformBlock<ContainerImage, ImageScanDetails> scannerBlock;
        private readonly ActionBlock<ImageScanDetails> exporterBlock;

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
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Complete().Wait();
        }
    }
}
