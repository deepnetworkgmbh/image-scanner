using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using core.core;
using core.exporters;
using core.images;
using core.scanners;
using Serilog;

namespace core
{
    public class KubeScanner
    {
        public async Task Scan(IScanner scanner, IExporter exporter, IImageProvider images, int parallelismDegree)
        {
            try
            {
                // create the pipeline of actions
                var scannerBlock = new TransformBlock<ContainerImage, ImageScanDetails>(
                    i => Transform(scanner, i),
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = parallelismDegree,
                    });

                var exporterBlock = new ActionBlock<ImageScanDetails>(
                    exporter.UploadAsync,
                    new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = parallelismDegree,
                    });

                // link the actions
                scannerBlock.LinkTo(
                    exporterBlock,
                    new DataflowLinkOptions
                    {
                        PropagateCompletion = true,
                    });

                // scan images in parallel
                foreach (var image in await images.GetImages())
                {
                    await scannerBlock.SendAsync(image);
                }

                scannerBlock.Complete();
                await exporterBlock.Completion;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to scan images");
            }

            // write finish message
            Log.Information("kube-scanner finished execution");
        }

        private static async Task<ImageScanDetails> Transform(IScanner scanner, ContainerImage image)
        {
            Log.Information("Scanning image {Image}", image.FullName);
            return await scanner.Scan(image);
        }
    }
}
