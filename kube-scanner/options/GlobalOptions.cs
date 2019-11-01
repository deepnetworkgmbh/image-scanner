using CommandLine;

namespace kube_scanner.options
{
    public abstract class GlobalOptions
    {
        [Option('k', "kubeConfigPath", Required = false, 
            HelpText = "File path of Kube Config file")]
        public string KubeConfigPath { get; set; }
        
        [Option('e', "exporter", Required = true, 
            HelpText = "Exporter type (e.g, File)")]
        public string Exporter { get; set; }
        
        [Option('f', "fileExporterPath", Required = false, 
            HelpText = "Folder path of file exporter")]
        public string FileExporterPath { get; set; }
        
        [Option('b', "isBulkUpload", Required = false, Default = false, 
            HelpText = "Is bulk upload")]
        public bool IsBulkUpload { get; set; }
        
        [Option('m', "maxParallelismPercentage", Required = false, Default = 10, 
            HelpText = "Maximum Degree of Parallelism in Percentage")]
        public int MaxParallelismPercentage { get; set; }
    }
}