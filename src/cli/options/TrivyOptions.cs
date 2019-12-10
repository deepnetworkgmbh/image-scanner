using CommandLine;

namespace cli.options
{
    [Verb("trivy", HelpText = "Run trivy scanner")]
    public class TrivyOptions : GlobalOptions
    {
        [Option('t', "trivyBinaryPath", Required = false, Default = "/usr/local/bin/trivy", HelpText = "Binary path of Trivy executable")]
        public string TrivyBinaryPath { get; set; }

        [Option('a', "trivyCachePath", Required = false, HelpText = "Folder path of Trivy cache files")]
        public string TrivyCachePath { get; set; }

        [Option('r', "registries", Required = false, HelpText = "The path of Container Registry Credentials file")]
        public string RegistriesFilePath { get; set; }
    }
}