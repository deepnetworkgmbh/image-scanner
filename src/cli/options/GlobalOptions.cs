using CommandLine;

namespace cli.options
{
    public abstract class GlobalOptions
    {
        [Option('k', "kubeConfigPath", Required = false, SetName = "KubeConfigPath", HelpText = "File path of Kube Config file")]
        public string KubeConfigPath { get; set; }

        [Option('e', "exporter", Required = true, HelpText = "Exporter type (e.g, File)")]
        public string Exporter { get; set; }

        [Option('i', "importer", Required = true, HelpText = "Importer type (e.g, File)")]
        public string Importer { get; set; }

        [Option('f', "filePath", Required = false, HelpText = "Folder path of file exporter and importer")]
        public string FileExporterImporterPath { get; set; }

        [Option('b', "isBulkUpload", Required = false, Default = false, HelpText = "Is bulk upload")]
        public bool IsBulkUpload { get; set; }

        [Option('m', "parallelismDegree", Required = false, Default = 10, HelpText = "Degree of Parallelism")]
        public int ParallelismDegree { get; set; }

        [Option('l', "listOfImagesPath", Required = false, SetName = "ImagesListPath", HelpText = "The path of images list file")]
        public string ImagesListFilePath { get; set; }
    }
}