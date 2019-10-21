using kube_scanner.core;

namespace kube_scanner.scanners
{
    public interface IScanner
    {
        ScanResult Scan(string imageTag);
    }
}