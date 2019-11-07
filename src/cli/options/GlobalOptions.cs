using CommandLine;

namespace cli.options
{
    public abstract class GlobalOptions
    {
        [Option('k', "kubeConfigPath", Required = false, HelpText = "File path of Kube Config file")]
        public string KubeConfigPath { get; set; }

        [Option('e', "exporter", Required = true, HelpText = "Exporter type (e.g, File)")]
        public string Exporter { get; set; }

        [Option('f', "fileExporterPath", Required = false, HelpText = "Folder path of file exporter")]
        public string FileExporterPath { get; set; }

        [Option('b', "isBulkUpload", Required = false, Default = false, HelpText = "Is bulk upload")]
        public bool IsBulkUpload { get; set; }

        [Option('m', "parallelismDegree", Required = false, Default = 10, HelpText = "Degree of Parallelism")]
        public int ParallelismDegree { get; set; }
    }
}