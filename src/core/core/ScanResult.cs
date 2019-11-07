using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace core.core
{
    public class ScanResult
    {
        [JsonProperty(PropertyName = "imageName")]
        public string ImageName { get; set; }
        
        
        [JsonProperty(PropertyName = "scanResultArray")]
        public JArray ScanResultArray { get; set; }
        
        [JsonProperty(PropertyName = "logs")]
        public string Logs { get; set; }
    }
}