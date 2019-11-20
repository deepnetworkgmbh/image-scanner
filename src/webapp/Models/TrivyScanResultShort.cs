using core.core;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace webapp.Models
{
    /// <summary>
    /// Represents short summary about a single Trivy Scan Result.
    /// </summary>
    public class TrivyScanResultShort
    {
        /// <summary>
        /// The full image name.
        /// </summary>
        [JsonProperty(PropertyName = "image")]
        public string Image { get; set; }

        /// <summary>
        /// Scan result.
        /// </summary>
        [JsonProperty(PropertyName = "scanResult")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ScanResult ScanResult { get; set; }

        /// <summary>
        /// Description for Failed scans.
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Counts issues by Severity.
        /// </summary>
        [JsonProperty(PropertyName = "counters")]
        public VulnerabilityCounters[] Counters { get; set; }
    }

    /// <summary>
    /// Represents the amount of issues with a particular severity.
    /// </summary>
    public class VulnerabilityCounters
    {
        /// <summary>
        /// The CVE severity.
        /// </summary>
        [JsonProperty(PropertyName = "severity")]
        public string Severity { get; set; }

        /// <summary>
        /// Amount of CVEs with this severity.
        /// </summary>
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }
    }
}