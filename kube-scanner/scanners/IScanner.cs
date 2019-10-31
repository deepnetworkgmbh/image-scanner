using System.Threading.Tasks;
using kube_scanner.core;

namespace kube_scanner.scanners
{
    public interface IScanner
    {
        string ContainerRegistryAddress { get; set; }
        string ContainerRegistryUserName { get; set; }
        string ContainerRegistryPassword { get; set; }
        
        Task<ScanResult> Scan(string imageTag);
    }
}