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
    public static class KubeScanner
    {
        public static async Task Scan(IScanner scanner, IExporter exporter, IImageProvider images, int parallelismDegree)
        {
            try
            {
                // create the pipeline of actions
                var scannerBlock = new TransformBlock<ContainerImage, ImageScanDetails>(
                    async i => await scanner.Scan(i), new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = parallelismDegree,
                    });

                var exporterBlock = new ActionBlock<ImageScanDetails>(
                    c => { exporter.UploadAsync(c); }, new ExecutionDataflowBlockOptions
                    {
                        MaxDegreeOfParallelism = parallelismDegree,
                    });

                // link the actions
                scannerBlock.LinkTo(
                    exporterBlock, new DataflowLinkOptions
                    {
                        PropagateCompletion = true,
                    });

                // scan images in parallel
                foreach (var image in await images.GetImages())
                {
                    Log.Information("Scanning image {Image}", image.FullName);

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
    }
}
