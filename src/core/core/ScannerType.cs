using System.Runtime.Serialization;

namespace core.core
{
    public enum ScannerType
    {
        [EnumMember(Value = "TRIVY")]
        Trivy = 1,

        [EnumMember(Value = "CLAIR")]
        Clair = 2,
    }
}