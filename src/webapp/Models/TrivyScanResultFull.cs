using core.core;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace webapp.Models
{
    /// <summary>
    /// Represents detailed information about a single Trivy Scan Result.
    /// </summary>
    public class TrivyScanResultFull
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
        /// List of scanned targets.
        /// </summary>
        [JsonProperty(PropertyName = "targets")]
        public TrivyScanTarget[] Targets { get; set; }
    }
}