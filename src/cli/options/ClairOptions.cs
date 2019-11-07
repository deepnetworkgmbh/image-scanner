using CommandLine;

namespace cli.options
{
    [Verb("clair", HelpText = "Run clair scanner")]
    public class ClairOptions : GlobalOptions
    {
    }
}