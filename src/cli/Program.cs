using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using cli.configuration;
using cli.options;
using CommandLine;

using core;
using core.exporters;
using core.images;
using core.importers;
using core.scanners;
using Microsoft.Extensions.Configuration;
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
                    .WithParsed<TrivyOptions>(options => RunTrivy(options).Wait())
                    .WithParsed<ClairOptions>(RunClair);
            }
            catch (NotImplementedException e)
            {
                Log.Fatal(e, "Not implemented functionality was requested");
                Environment.Exit(1);
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Unexpected exception");
                Environment.Exit(1);
            }
        }

        private static void RunClair(ClairOptions clairOptions)
        {
            throw new NotImplementedException();
        }

        private static async Task RunTrivy(TrivyOptions trivyOptions)
        {
            var exporter = InitializeExporter(trivyOptions);
            var importer = InitializeImporter(trivyOptions);
            IImageProvider imageProvider;

            // if a list of images is provided, scan the list. If not, scan the k8s cluster images
            if (!string.IsNullOrEmpty(trivyOptions.ImagesListFilePath))
            {
                var images = File.ReadAllLines(trivyOptions.ImagesListFilePath);
                imageProvider = new InMemoryImageProvider(images);
            }
            else
            {
                imageProvider = new KubernetesImageProvider(trivyOptions.KubeConfigPath);
            }

            // read private container registry credentials
            var registries = new RegistryCredentials[1];
            if (!string.IsNullOrEmpty(trivyOptions.RegistriesFilePath))
            {
                var registryConfig = new RegistryConfigurationParser(trivyOptions.RegistriesFilePath).Get();

                registries = registryConfig.Registries;
            }

            var scanner = new Trivy(trivyOptions.TrivyCachePath, trivyOptions.TrivyBinaryPath, registries);

            using var imageScanner = new ImageScanner(scanner, exporter, importer, trivyOptions.ParallelismDegree, 1000);
            await imageScanner.Scan(imageProvider);
        }

        private static IExporter InitializeExporter(GlobalOptions options)
        {
            try
            {
                return options.Exporter switch
                {
                    "File" => new FileExporter(options.FileExporterImporterPath),
                    _ => throw new Exception("unsupported exporter: " + options.Exporter)
                };
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Failed to initialize Exporter");
                throw;
            }
        }

        private static IImporter InitializeImporter(GlobalOptions options)
        {
            try
            {
                return options.Importer switch
                {
                    "File" => new FileImporter(options.FileExporterImporterPath),
                    _ => throw new Exception("unsupported importer: " + options.Exporter)
                };
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Failed to initialize Importer");
                throw;
            }
        }
    }
}