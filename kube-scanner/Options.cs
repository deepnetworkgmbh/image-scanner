using CommandLine;

namespace kube_scanner
{
    public class Options
    {
        [Option('k', "kubeConfigPath", Required = false, HelpText = "File path of Kube Config file")]
        public string KubeConfigPath { get; set; }
        
        [Option('s', "scanner", Required = true, HelpText = "Scanner type (Trivy or Clair")]
        public string Scanner { get; set; }        
        
        [Option('e', "exporter", Required = true, HelpText = "Exporter type (File or ..")]
        public string Exporter { get; set; }
        
        [Option('m', "maxParallelismPercentage", Required = true, HelpText = "Maximum Degree of Parallelism in Percentage")]
        public int MaxParallelismPercentage { get; set; }

        [Option('c', "containerRegistryAddress", Required = false, HelpText = "Container Registry Address")]
        public string ContainerRegistryAddress { get; set; }
        
        [Option('u', "containerRegistryUserName", Required = false, HelpText = "Container Registry User Name")]
        public string ContainerRegistryUserName { get; set; }
        
        [Option('p', "containerRegistryPassword", Required = false, HelpText = "Container Registry User Password")]
        public string ContainerRegistryPassword { get; set; }
    }
}