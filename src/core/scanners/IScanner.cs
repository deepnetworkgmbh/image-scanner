using System.Threading.Tasks;
using core.core;

namespace core.scanners
{
    public interface IScanner
    {
        Task<ScanResult> Scan(string imageTag);
    }
}