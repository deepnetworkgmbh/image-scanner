using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.exporters;
using core.helpers;
using core.scanners;
using core.core;

namespace core
{
    public static class MainClass
    {
        private static readonly List<ScanResult>
            ScanResults = new List<ScanResult>(); // list of scan results      

        public static void Main(IScanner scanner, IExporter exporter, IEnumerable<string> images, int parallelismDegree)
        {
            var opt = new ParallelOptions {MaxDegreeOfParallelism = parallelismDegree};

            // scan the images in parallel and save results into the exporter
            Parallel.ForEach(images, opt, image =>
            {
                try
                {
                    LogHelper.LogMessages("Scanning image", image);

                    var task = Task.Run(async () => await scanner.Scan(image));
                    var result = task.Result;

                    if (exporter.IsBulkUpload)
                        ScanResults.Add(result);
                    else
                        exporter.Upload(result);
                }
                catch (AggregateException ex)
                {
                    foreach (var innerException in ex.InnerExceptions)
                    {
                        LogHelper.LogErrorsAndContinue(
                            $"Failed to scan image {image} due {innerException.GetType()} exception{Environment.NewLine}{innerException.Message}");
                    }
                }
            });

            // if bulk upload is selected
            if (exporter.IsBulkUpload)
            {
                exporter.UploadBulk(ScanResults);
            }
            
            // write finish message
            LogHelper.LogMessages("kube-scanner finished execution");
        }
    }
}
