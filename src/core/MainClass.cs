using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.core;
using core.exporters;
using core.scanners;
using Serilog;

namespace core
{
    public static class MainClass
    {
        private static readonly List<ScanResult>
            ScanResults = new List<ScanResult>(); // list of scan results

        public static void Main(IScanner scanner, IExporter exporter, IEnumerable<string> images, int parallelismDegree)
        {
            var opt = new ParallelOptions { MaxDegreeOfParallelism = parallelismDegree };

            // scan the images in parallel and save results into the exporter
            Parallel.ForEach(images, opt, image =>
            {
                try
                {
                    Log.Information("Scanning image {Message}", image);

                    var task = Task.Run(async () => await scanner.Scan(image));
                    var result = task.Result;

                    if (exporter.IsBulkUpload)
                    {
                        ScanResults.Add(result);
                    }
                    else
                    {
                        exporter.Upload(result);
                    }
                }
                catch (AggregateException ex)
                {
                    foreach (var innerException in ex.InnerExceptions)
                    {
                        var exType = innerException.GetType();
                        var innerExMessage = innerException.Message;
                        Log.Error(
                            "Failed to scan image {Image} due {Message} exception {Exception}",
                            image,
                            exType,
                            innerExMessage);
                    }
                }
            });

            // if bulk upload is selected
            if (exporter.IsBulkUpload)
            {
                exporter.UploadBulk(ScanResults);
            }

            // write finish message
            Log.Information("kube-scanner finished execution");
        }
    }
}
