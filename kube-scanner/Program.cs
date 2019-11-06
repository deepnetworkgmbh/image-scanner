using System;
using System.Collections.Generic;
using CommandLine;
using kube_scanner.core;
using kube_scanner.exporters;
using kube_scanner.scanners;
using System.Threading.Tasks;
using kube_scanner.helpers;
using kube_scanner.options;

namespace kube_scanner
{
    internal static class Program
    {
        private static readonly List<ScanResult> 
            ScanResults = new List<ScanResult>(); // list of scan results
        
        private static IEnumerable<string> _images; // list of unique images 
        
        private static IScanner _scanner;
        
        private static IExporter _exporter;
        

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

        private static void RunTrivy(TrivyOptions trivyOptions)
        {
            // create kube client and get the list of unique images
            RetrieveImagesFromKube(trivyOptions);
            
            // create the scanner object and set private cr credentials
            _scanner = new Trivy(trivyOptions.TrivyCachePath)
            {
                // set private container registry credentials
                ContainerRegistryAddress = trivyOptions.ContainerRegistryAddress,
                ContainerRegistryUserName = trivyOptions.ContainerRegistryUserName,
                ContainerRegistryPassword = trivyOptions.ContainerRegistryPassword
            };

            // run scanner and upload outputs into exporter
            RunScannerAndUpload(trivyOptions);
            
            // write finish message
            LogHelper.LogMessages("kube-scanner finished execution");
        }

        private static void RetrieveImagesFromKube(GlobalOptions options)
        {
            try
            {
                // create the exporter object
                _exporter = options.Exporter == "File" ? new FileExporter(options.FileExporterPath) 
                    : throw new Exception("unsupported exporter: "+ options.Exporter);
            }
            catch (Exception e)
            {
                LogHelper.LogErrorsAndExit(e.Message);
                Environment.Exit(1);
            }

            // create a Kubernetes client
            var kubeClient = new KubeClient(options.KubeConfigPath);
            
            // retrieve the unique list of images in the cluster
            _images = kubeClient.GetImages(); 
        }
        
        private static void RunScannerAndUpload(GlobalOptions options)
        {
            var opt = new ParallelOptions { MaxDegreeOfParallelism = options.ParallelismDegree };

            // scan the images in parallel and save results into the exporter
            Parallel.ForEach(_images, opt, (image) =>
            {
                LogHelper.LogMessages("Scanning image", image);
                
                var task = Task.Run(async () => await _scanner.Scan(image));
                var result = task.Result;
                if (options.IsBulkUpload)
                    ScanResults.Add(result);
                else
                    _exporter.Upload(result);
            });
            
            // if bulk upload is selected
            if (options.IsBulkUpload)
            {
                _exporter.UploadBulk(ScanResults);
            }
        }
    }
}