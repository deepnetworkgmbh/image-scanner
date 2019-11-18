using System;
using System.Threading.Tasks;

using cli.options;
using CommandLine;

using core;
using core.exporters;
using core.images;
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
                    .WithParsed<TrivyOptions>(async options => await RunTrivy(options))
                    .WithParsed<ClairOptions>(RunClair);
            }
            catch (NotImplementedException e)
            {
                Log.Fatal(e, "Not implemented functionality was requested");
            }
            catch (Exception e)
            {
                Log.Fatal(e, "Unexpected exception");
            }
        }

        private static void RunClair(ClairOptions clairOptions)
        {
            throw new NotImplementedException();
        }

        private static async Task RunTrivy(TrivyOptions trivyOptions)
        {
            var exporter = InitializeExporter(trivyOptions);

            var imageProvider = new KubernetesImageProvider(trivyOptions.KubeConfigPath);

            // set private container registry credentials
            var scanner = new Trivy(trivyOptions.TrivyCachePath)
            {
                ContainerRegistryAddress = trivyOptions.ContainerRegistryAddress,
                ContainerRegistryUserName = trivyOptions.ContainerRegistryUserName,
                ContainerRegistryPassword = trivyOptions.ContainerRegistryPassword,
                TrivyBinaryPath = trivyOptions.TrivyBinaryPath,
            };

            // run core project's main
            await KubeScanner.Scan(scanner, exporter, imageProvider, trivyOptions.ParallelismDegree);
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
                Log.Fatal(e, "Failed to initialize Exporter");
                Environment.Exit(1);
            }

            exporter.IsBulkUpload = options.IsBulkUpload;

            return exporter;
        }
    }
}