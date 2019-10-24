using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using kube_scanner.core;
using kube_scanner.exporters;
using kube_scanner.scanners;
using System.Threading.Tasks;

namespace kube_scanner
{
    class Program
    {
        private static void Main(string[] args)
        {
                var result = Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(RunProgram)
                    .WithNotParsed(LogErrors);
        }

        private static void RunProgram(Options options)
        {
            // get the folder path to store exported files
            var folderPath = Directory.GetCurrentDirectory();
            
            // create a Kubernetes client
            var kubeClient = new KubeClient(options.KubeConfigPath);
            
            // retrieve the unique list of images in the cluster
            var images = kubeClient.GetImages();
            
            // create the scanner object
            var scanner = options.Scanner == "Trivy" ? new Trivy() : throw new Exception("not supported scanner!");

            // create the exporter object
            var exporter = options.Exporter == "File" ? new FileExporter(folderPath) : throw new Exception("not supported exporter!");

            // calculate max degree of parallelism and create the parallel options object
            var mdop = CalculateMaxDegreeOfParallelism(options.MaxParallelismPercentage);
            var opt = new ParallelOptions {MaxDegreeOfParallelism = mdop};
            
            // scan the images in parallel and save results into the exporter
            Parallel.ForEach(images, opt, (image) =>
            {
                Console.WriteLine("Scanning image: {0}", image);
                var task = Task.Run<ScanResult>(async () => await scanner.Scan(image));
                var result = task.Result;
                exporter.Upload(result);
            });
        }

        private static int CalculateMaxDegreeOfParallelism(int maxParallelismPercent)
        {
            if ((Environment.ProcessorCount) == 0)
                return -1; // there is no limit on the number of concurrently running operations
            
            var floatDegree = (float) ((Environment.ProcessorCount) * maxParallelismPercent) / 100;
            var intDegree = (int) Math.Ceiling(floatDegree);

            return intDegree;
        }

        private static void LogErrors(IEnumerable<Error> errors)
        {
            System.Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine("Program wasn't able to parse your input");
            foreach (var error in errors)
            {
                System.Console.WriteLine(error);
            }

            System.Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}