using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using kube_scanner.core;
using Docker.DotNet;
using Docker.DotNet.Models;
using kube_scanner.exporters;
using kube_scanner.scanners;

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
            var folderPath = Directory.GetCurrentDirectory();
            
            var kubeClient = new KubeClient(options.KubeConfigPath);
            
            var images = kubeClient.GetImages();
            
            var scanner = options.Scanner == "Trivy" ? new Trivy() : throw new Exception("not supported scanner!");

            var exporter = options.Exporter == "File" ? new FileExporter(folderPath) : throw new Exception("not supported exporter!");
            
            foreach (var image in images)
            {
                Console.WriteLine("Scanning image: {0}", image);
                var result = scanner.Scan(image);
                exporter.Upload(result);
            }
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