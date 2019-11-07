using System.Collections.Generic;
using System.Threading.Tasks;
using core.core;
using core.scanners;

namespace core.exporters
{
    public interface IExporter
    {
        Task UploadAsync(ImageScanDetails details);

        Task UploadBulkAsync(IEnumerable<ImageScanDetails> results);

        bool IsBulkUpload { get; set; }
    }
}