using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Helpers;
using ABCRetailers.Functions.Models;
using System.Net;

namespace ABCRetailers.Functions.Functions
{
    public class UploadsFunctions
    {
        private readonly ILogger _logger;
        private readonly ShareServiceClient _shareServiceClient;

        public UploadsFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UploadsFunctions>();
            
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection") 
                ?? throw new InvalidOperationException("AzureStorageConnection is not configured");
            
            _shareServiceClient = new ShareServiceClient(connectionString);
        }

        /// <summary>
        /// Upload file to Azure File Share
        /// </summary>
        [Function("UploadFile")]
        public async Task<HttpResponseData> UploadFile(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/upload")] HttpRequestData req)
        {
            _logger.LogInformation("Uploading file to Azure File Share");

            try
            {
                var (fields, files) = await MultipartHelper.ParseMultipartAsync(req);

                if (!files.Any())
                {
                    return await HttpJson.WriteErrorAsync(req, "No file provided");
                }

                var shareName = fields.GetValueOrDefault("shareName", "payment-proofs");
                var directoryName = fields.GetValueOrDefault("directoryName", "uploads");

                var fileEntry = files.First();
                var fileStream = fileEntry.Value;
                var originalFileName = fields.GetValueOrDefault("fileName", "upload.dat");

                var fileUrl = await UploadFileToShareAsync(fileStream, shareName, directoryName, originalFileName);

                var response = new FileUploadResponse
                {
                    FileUrl = fileUrl,
                    FileName = originalFileName
                };

                return await HttpJson.WriteJsonAsync(req, response, HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Upload file to multiple file shares (for contracts and payment-proofs)
        /// </summary>
        [Function("UploadFileMultiple")]
        public async Task<HttpResponseData> UploadFileMultiple(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "files/upload-multiple")] HttpRequestData req)
        {
            _logger.LogInformation("Uploading file to multiple Azure File Shares");

            try
            {
                var (fields, files) = await MultipartHelper.ParseMultipartAsync(req);

                if (!files.Any())
                {
                    return await HttpJson.WriteErrorAsync(req, "No file provided");
                }

                var fileEntry = files.First();
                var fileStream = fileEntry.Value;
                var originalFileName = fields.GetValueOrDefault("fileName", "upload.dat");

                // Copy stream for multiple uploads
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Upload to payment-proofs share
                var paymentProofsStream = new MemoryStream(memoryStream.ToArray());
                var paymentProofsUrl = await UploadFileToShareAsync(paymentProofsStream, "payment-proofs", "uploads", originalFileName);

                // Upload to contracts share
                memoryStream.Position = 0;
                var contractsStream = new MemoryStream(memoryStream.ToArray());
                var contractsUrl = await UploadFileToShareAsync(contractsStream, "contracts", "payments", originalFileName);

                var response = new
                {
                    PaymentProofsUrl = paymentProofsUrl,
                    ContractsUrl = contractsUrl,
                    FileName = originalFileName
                };

                return await HttpJson.WriteJsonAsync(req, response, HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file to multiple shares");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// List files in a file share directory
        /// </summary>
        [Function("ListFiles")]
        public async Task<HttpResponseData> ListFiles(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "files/{shareName}/{directoryName}")] HttpRequestData req,
            string shareName,
            string directoryName)
        {
            _logger.LogInformation($"Listing files in share: {shareName}, directory: {directoryName}");

            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);

                var files = new List<string>();
                await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        files.Add(item.Name);
                    }
                }

                return await HttpJson.WriteJsonAsync(req, files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing files");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Delete file from Azure File Share
        /// </summary>
        [Function("DeleteFile")]
        public async Task<HttpResponseData> DeleteFile(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "files/{shareName}/{directoryName}/{fileName}")] HttpRequestData req,
            string shareName,
            string directoryName,
            string fileName)
        {
            _logger.LogInformation($"Deleting file: {fileName} from share: {shareName}, directory: {directoryName}");

            try
            {
                var shareClient = _shareServiceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                var fileClient = directoryClient.GetFileClient(fileName);

                await fileClient.DeleteIfExistsAsync();

                var response = req.CreateResponse(HttpStatusCode.NoContent);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        // Helper method to upload file to file share
        private async Task<string> UploadFileToShareAsync(Stream fileStream, string shareName, string directoryName, string originalFileName)
        {
            var shareClient = _shareServiceClient.GetShareClient(shareName);
            
            // Create share if it doesn't exist
            await shareClient.CreateIfNotExistsAsync();

            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            var fileName = $"{Guid.NewGuid()}_{originalFileName}";
            var fileClient = directoryClient.GetFileClient(fileName);

            // Get file size
            fileStream.Position = 0;
            var fileSize = fileStream.Length;

            await fileClient.CreateAsync(fileSize);
            await fileClient.UploadRangeAsync(new Azure.HttpRange(0, fileSize), fileStream);

            return fileClient.Uri.ToString();
        }
    }
}

