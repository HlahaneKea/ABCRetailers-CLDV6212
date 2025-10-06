using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;

namespace ABCRetailers.Functions.Helpers
{
    public class MultipartHelper
    {
        public static async Task<(Dictionary<string, string> fields, Dictionary<string, Stream> files)> ParseMultipartAsync(HttpRequestData req)
        {
            var fields = new Dictionary<string, string>();
            var files = new Dictionary<string, Stream>();

            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();
            if (string.IsNullOrEmpty(contentType))
            {
                return (fields, files);
            }

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType));
            var reader = new MultipartReader(boundary, req.Body);

            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var contentDisposition);

                if (hasContentDispositionHeader && contentDisposition != null)
                {
                    if (contentDisposition.DispositionType.Equals("form-data"))
                    {
                        var nameValue = contentDisposition.Name.HasValue ? contentDisposition.Name.Value : string.Empty;
                        var name = nameValue.Trim('"');

                        var hasFileName = contentDisposition.FileName.HasValue && !string.IsNullOrEmpty(contentDisposition.FileName.Value);
                        if (hasFileName)
                        {
                            // This is a file
                            var memoryStream = new MemoryStream();
                            await section.Body.CopyToAsync(memoryStream);
                            memoryStream.Position = 0;
                            files[name] = memoryStream;
                        }
                        else
                        {
                            // This is a form field
                            using var streamReader = new StreamReader(section.Body);
                            var value = await streamReader.ReadToEndAsync();
                            fields[name] = value;
                        }
                    }
                }

                section = await reader.ReadNextSectionAsync();
            }

            return (fields, files);
        }

        private static string GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;
            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }
            return boundary;
        }
    }
}

