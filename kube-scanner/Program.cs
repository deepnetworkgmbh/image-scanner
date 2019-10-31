using System;
using System.Collections.Generic;
using CommandLine;
using kube_scanner.core;
using kube_scanner.exporters;
using kube_scanner.scanners;
using System.Threading.Tasks;
using kube_scanner.helpers;

namespace kube_scanner
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(RunProgram)
                    .WithNotParsed(LogHelper.LogErrors);
        }

        private static void RunProgram(Options options)
        {
            var scanResults = new List<ScanResult>(); // list of scan results
            
            // initialize variables
            IScanner scanner = null;
            IExporter exporter = null;
            
            try
            {
                // create the scanner object
                scanner = options.Scanner == "Trivy" ? new Trivy(options.TrivyCachePath) : throw new Exception("unsupported scanner: "+ options.Scanner);
                
                // set private container registry credentials
                scanner.ContainerRegistryAddress  = options.ContainerRegistryAddress;
                scanner.ContainerRegistryUserName = options.ContainerRegistryUserName;
                scanner.ContainerRegistryPassword = options.ContainerRegistryPassword;
                
                // create the exporter object
                exporter = options.Exporter == "File" ? new FileExporter(options.FileExporterPath) : throw new Exception("unsupported exporter: "+ options.Exporter);
            }
            catch (Exception e)
            {
                LogHelper.LogErrorsAndExit(e.Message);
                Environment.Exit(1);
            }

            // create a Kubernetes client
            var kubeClient = new KubeClient(options.KubeConfigPath);
            
            // retrieve the unique list of images in the cluster
            var images = kubeClient.GetImages();
            
            // calculate max degree of parallelism and create the parallel options object
            var mdop = CalculateMaxDegreeOfParallelism(options.MaxParallelismPercentage);
            var opt = new ParallelOptions {MaxDegreeOfParallelism = mdop};
            
            // write start messages
            LogHelper.LogMessages("Kube-Scanner is running on parallelization degree", options.MaxParallelismPercentage);
            LogHelper.LogMessages("The number of logical CPU is", Environment.ProcessorCount);
            LogHelper.LogMessages("The number of CPU being used is", mdop);
            
            // scan the images in parallel and save results into the exporter
            Parallel.ForEach(images, opt, (image) =>
            {
                LogHelper.LogMessages("Scanning image", image);
                
                var task = Task.Run(async () => await scanner.Scan(image));
                var result = task.Result;
                if (options.IsBulkUpload)
                    scanResults.Add(result);
                else
                    exporter.Upload(result);
            });

            // if bulk upload is selected
            if (options.IsBulkUpload)
            {
                exporter.UploadBulk(scanResults);
            }
            
            // write finish message
            LogHelper.LogMessages("Kube-Scanner finished execution");

        }

        private static int CalculateMaxDegreeOfParallelism(int maxParallelismPercent)
        {
            var pc = Environment.ProcessorCount;

            if (pc == 0) return -1; // there is no limit on the number of concurrently running operations
            
            if ((maxParallelismPercent > 100) || (maxParallelismPercent < 1))
            {
                LogHelper.LogErrorsAndExit(
                    "Parallelism percent should be between 1-100. You typed",
                    maxParallelismPercent
                );
            }
            
            var floatDegree = (float) (pc * maxParallelismPercent) / 100;
            var intDegree = (int) Math.Ceiling(floatDegree);

            return intDegree;
        }
    }
}