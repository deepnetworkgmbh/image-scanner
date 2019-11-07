using System;
using System.Collections.Generic;
using CommandLine;
using cli.options;
using core.core;
using core.exporters;
using core.helpers;
using core.scanners;
using Parser = CommandLine.Parser;


namespace cli
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Parser.Default.ParseArguments<TrivyOptions, ClairOptions>(args)
                    .WithParsed<TrivyOptions>(RunTrivy)
                    .WithParsed<ClairOptions>(RunClair);
            }
            catch (NotImplementedException e)
            {
                LogHelper.LogErrorsAndExit(e.Message);
            }
            catch (Exception e)
            {
                LogHelper.LogErrorsAndExit("Something went wrong!", e.Message);
            }
        }

        private static void RunClair(ClairOptions obj)
        {
            throw new NotImplementedException();
        }

        private static void RunTrivy(GlobalOptions globalOptions)
        {
            var options = globalOptions as TrivyOptions;

            // create kube client and get the list of unique images
            var imageList = RetrieveImagesFromKube(options);

            if (options == null) return;
            var scanner = new Trivy(options.TrivyCachePath)
            {
                ContainerRegistryAddress = options.ContainerRegistryAddress,
                ContainerRegistryUserName = options.ContainerRegistryUserName,
                ContainerRegistryPassword = options.ContainerRegistryPassword
            };

            var exporter = InitializeExporter(options);

            core.MainClass.Main(scanner, exporter, imageList, options.ParallelismDegree);


        }

        private static IEnumerable<string> RetrieveImagesFromKube(GlobalOptions options)
        {
            // create a Kubernetes client
            var kubeClient = new KubeClient(options.KubeConfigPath);
            
            // retrieve the unique list of images in the cluster
            var images = kubeClient.GetImages();

            return images;
        }
        
        private static IExporter InitializeExporter(GlobalOptions options)
        {
            IExporter exporter = null;
            
            try
            {
                // create the exporter object
                exporter = options.Exporter == "File" ? new FileExporter(options.FileExporterPath) 
                    : throw new Exception("unsupported exporter: "+ options.Exporter);
            }
            catch (Exception e)
            {
                LogHelper.LogErrorsAndExit(e.Message);
                Environment.Exit(1);
            }

            exporter.IsBulkUpload = options.IsBulkUpload;
            
            return exporter;
        }
    }
}