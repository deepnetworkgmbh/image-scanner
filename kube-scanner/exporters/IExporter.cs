using System.Collections.Generic;
using kube_scanner.core;

namespace kube_scanner.exporters
{
    public interface IExporter
    {
        void Upload(ScanResult result);

        void UploadBulk(IEnumerable<ScanResult> results);
    }
}