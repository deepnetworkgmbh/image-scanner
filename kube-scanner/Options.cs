using CommandLine;

namespace kube_scanner
{
    public class Options
    {
        [Option('a', "trivyCachePath", Required = false, HelpText = "Folder path of Trivy cache files")]
        public string TrivyCachePath { get; set; }
        
        [Option('f', "fileExporterPath", Required = false, HelpText = "Folder path of file exporter")]
        public string FileExporterPath { get; set; }

        [Option('k', "kubeConfigPath", Required = false, HelpText = "File path of Kube Config file")]
        public string KubeConfigPath { get; set; }
        
        [Option('s', "scanner", Required = true, HelpText = "Scanner type (e.g, Trivy")]
        public string Scanner { get; set; }        
        
        [Option('e', "exporter", Required = true, HelpText = "Exporter type (e.g, File")]
        public string Exporter { get; set; }
        
        [Option('m', "maxParallelismPercentage", Required = false, Default = 10, HelpText = "Maximum Degree of Parallelism in Percentage")]
        public int MaxParallelismPercentage { get; set; }

        [Option('c', "containerRegistryAddress", Required = false, HelpText = "Container Registry Address")]
        public string ContainerRegistryAddress { get; set; }
        
        [Option('u', "containerRegistryUserName", Required = false, HelpText = "Container Registry User Name")]
        public string ContainerRegistryUserName { get; set; }
        
        [Option('p', "containerRegistryPassword", Required = false, HelpText = "Container Registry User Password")]
        public string ContainerRegistryPassword { get; set; }
        
        [Option('b', "isBulkUpload", Required = false, Default = false, HelpText = "Is bulk upload")]
        public bool IsBulkUpload { get; set; }
    }
}