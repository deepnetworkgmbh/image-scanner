using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using SharpCompress.Readers;

namespace core.helpers
{
    public static class TarHelper
    {
        public static JArray UnTarIntoJsonArray(Stream stream)
        {
            var memoryStream = new MemoryStream();
            
            using (var reader = ReaderFactory.Open(stream))
            {
                while (reader.MoveToNextEntry())
                {
                    if (reader.Entry.IsDirectory) continue;
                    using var entryStream = reader.OpenEntryStream();
                    entryStream.CopyTo(memoryStream);
                }
            }
                   
            var buffer = memoryStream.GetBuffer();
            
            var jsonString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

            if (string.IsNullOrEmpty(jsonString)) 
                jsonString = "[]";
            
            var jsonArray = JArray.Parse(jsonString); 
            
            return jsonArray;
        }
    }
}