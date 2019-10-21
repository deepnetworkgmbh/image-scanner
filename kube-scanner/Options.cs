using CommandLine;

namespace kube_scanner
{
    public class Options
    {
        [Option('p', "kubeConfigPath", Required = false, HelpText = "File path of Kube Config file")]
        public string KubeConfigPath { get; set; }
        
        [Option('s', "scanner", Required = true, HelpText = "Scanner type (Trivy or Clair")]
        public string Scanner { get; set; }        
        
        [Option('e', "exporter", Required = true, HelpText = "Exporter type (File or ..")]
        public string Exporter { get; set; }
    }
}