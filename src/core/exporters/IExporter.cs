using System.Collections.Generic;
using System.Threading.Tasks;
using core.core;

namespace core.exporters
{
    public interface IExporter
    {
        Task UploadAsync(ScanResult result);

        Task UploadBulkAsync(IEnumerable<ScanResult> results);

        bool IsBulkUpload { get; set; }
    }
}