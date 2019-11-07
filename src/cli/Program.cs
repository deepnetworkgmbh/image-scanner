using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using cli.options;
using CommandLine;
using core.core;
using core.exporters;
using core.scanners;
using Serilog;
using Parser = CommandLine.Parser;

namespace cli
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Information()
                    .WriteTo.Console(outputTemplate:
                        "[{Timestamp:HH:mm:ss} {Level}] {Message:lj}{NewLine}{Exception}")
                    .CreateLogger();

                Parser.Default.ParseArguments<TrivyOptions, ClairOptions>(args)
                    .WithParsed<TrivyOptions>(RunTrivy)
                    .WithParsed<ClairOptions>(RunClair);
            }
            catch (NotImplementedException e)
            {
                Log.Fatal("{Message}", e.Message);
            }
            catch (Exception e)
            {
                Log.Fatal("Something went wrong! {Message}", e.Message);
            }
        }

        private static void RunClair(ClairOptions clairOptions)
        {
            throw new NotImplementedException();
        }

        private static void RunTrivy(TrivyOptions trivyOptions)
        {
            // create kube-client and get the list of unique images
            var imageList = RetrieveImagesFromKube(trivyOptions.KubeConfigPath).Result;

            // set private container registry credentials
            var scanner = new Trivy(trivyOptions.TrivyCachePath)
            {
                ContainerRegistryAddress = trivyOptions.ContainerRegistryAddress,
                ContainerRegistryUserName = trivyOptions.ContainerRegistryUserName,
                ContainerRegistryPassword = trivyOptions.ContainerRegistryPassword,
            };

            // create exporter object
            var exporter = InitializeExporter(trivyOptions);

            // run core project's main
            core.MainClass.Main(scanner, exporter, imageList, trivyOptions.ParallelismDegree);
        }

        private static async Task<IEnumerable<ContainerImage>> RetrieveImagesFromKube(string kubeConfigPath)
        {
            // create a Kubernetes client (factory pattern)
            var kubeClient = await KubeClient.CreateAsync(kubeConfigPath);

            // retrieve the unique list of images in the cluster
            var images = await kubeClient.GetImages();

            return images;
        }

        private static IExporter InitializeExporter(GlobalOptions options)
        {
            IExporter exporter = null;

            try
            {
                // create the exporter object
                exporter = options.Exporter == "File" ? new FileExporter(options.FileExporterPath)
                    : throw new Exception("unsupported exporter: " + options.Exporter);
            }
            catch (Exception e)
            {
                Log.Fatal("{Message}", e.Message);
                Environment.Exit(1);
            }

            exporter.IsBulkUpload = options.IsBulkUpload;

            return exporter;
        }
    }
}