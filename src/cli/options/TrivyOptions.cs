using CommandLine;

namespace cli.options
{
    [Verb("trivy", HelpText = "Run trivy scanner")]
    public class TrivyOptions : GlobalOptions {
        [Option('a', "trivyCachePath", Required = false, HelpText = "Folder path of Trivy cache files")]
        public string TrivyCachePath { get; set; }
        
        [Option('c', "containerRegistryAddress", Required = false, HelpText = "Container Registry Address")]
        public string ContainerRegistryAddress { get; set; }
        
        [Option('u', "containerRegistryUserName", Required = false, HelpText = "Container Registry User Name")]
        public string ContainerRegistryUserName { get; set; }
        
        [Option('p', "containerRegistryPassword", Required = false, HelpText = "Container Registry User Password")]
        public string ContainerRegistryPassword { get; set; }
    }
}