using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using core.core;
using core.exporters;
using core.scanners;
using Serilog;

namespace core
{
    public static class MainClass
    {
        public static void Main(IScanner scanner, IExporter exporter, IEnumerable<ContainerImage> images, int parallelismDegree)
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
                foreach (var image in images)
                {
                    Log.Information("Scanning image {Message}", image);

                    scannerBlock.SendAsync(image);
                }

                scannerBlock.Complete();
                exporterBlock.Completion.Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var innerException in ex.InnerExceptions)
                {
                    var exType = innerException.GetType();
                    var innerExMessage = innerException.Message;
                    Log.Error(
                        "Failed to scan image due {ExType} exception {InnerExMessage}", exType, innerExMessage);
                }
            }

            // write finish message
            Log.Information("kube-scanner finished execution");
        }
    }
}
