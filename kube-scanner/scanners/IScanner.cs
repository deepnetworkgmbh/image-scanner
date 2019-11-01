using System.Threading.Tasks;
using kube_scanner.core;

namespace kube_scanner.scanners
{
    public interface IScanner
    {
        Task<ScanResult> Scan(string imageTag);
    }
}