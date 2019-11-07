using System.Collections.Generic;
using core.core;

namespace core.exporters
{
    public interface IExporter
    {
        void Upload(ScanResult result);

        void UploadBulk(IEnumerable<ScanResult> results);

        bool IsBulkUpload { get; set; }
    }
}