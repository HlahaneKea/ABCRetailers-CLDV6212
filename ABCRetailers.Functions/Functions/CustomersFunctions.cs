using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Helpers;
using ABCRetailers.Functions.Models;
using System.Net;

namespace ABCRetailers.Functions.Functions
{
    public class CustomersFunctions
    {
        private readonly ILogger _logger;
        private readonly TableServiceClient _tableServiceClient;

        public CustomersFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<CustomersFunctions>();
            
            // Get connection string from environment variables
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection") 
                ?? throw new InvalidOperationException("AzureStorageConnection is not configured");
            
            _tableServiceClient = new TableServiceClient(connectionString);
        }

        [Function("GetCustomers")]
        public async Task<HttpResponseData> GetCustomers(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all customers");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient("customers");
                await tableClient.CreateIfNotExistsAsync();

                var customers = new List<CustomerDto>();
                await foreach (var customer in tableClient.QueryAsync<CustomerEntity>())
                {
                    customers.Add(customer.ToDto());
                }

                return await HttpJson.WriteJsonAsync(req, customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customers");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("GetCustomer")]
        public async Task<HttpResponseData> GetCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "customers/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation($"Getting customer with ID: {id}");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient("customers");
                var response = await tableClient.GetEntityAsync<CustomerEntity>("customers", id);
                
                return await HttpJson.WriteJsonAsync(req, response.Value.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving customer {id}");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.NotFound);
            }
        }

        [Function("CreateCustomer")]
        public async Task<HttpResponseData> CreateCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "customers")] HttpRequestData req)
        {
            _logger.LogInformation("Creating new customer");

            try
            {
                var customerDto = await HttpJson.ReadJsonAsync<CustomerDto>(req);
                if (customerDto == null)
                {
                    return await HttpJson.WriteErrorAsync(req, "Invalid customer data");
                }

                // Generate unique ID
                string customerId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";
                customerDto.CustomerId = customerId;
                customerDto.RowKey = customerId;
                customerDto.PartitionKey = "customers";

                var entity = customerDto.ToEntity();
                
                var tableClient = _tableServiceClient.GetTableClient("customers");
                await tableClient.CreateIfNotExistsAsync();
                await tableClient.AddEntityAsync(entity);

                return await HttpJson.WriteJsonAsync(req, entity.ToDto(), HttpStatusCode.Created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating customer");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("UpdateCustomer")]
        public async Task<HttpResponseData> UpdateCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "customers/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation($"Updating customer with ID: {id}");

            try
            {
                var customerDto = await HttpJson.ReadJsonAsync<CustomerDto>(req);
                if (customerDto == null)
                {
                    return await HttpJson.WriteErrorAsync(req, "Invalid customer data");
                }

                customerDto.CustomerId = id;
                customerDto.RowKey = id;
                customerDto.PartitionKey = "customers";

                var entity = customerDto.ToEntity();
                
                var tableClient = _tableServiceClient.GetTableClient("customers");
                await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);

                return await HttpJson.WriteJsonAsync(req, entity.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating customer {id}");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("DeleteCustomer")]
        public async Task<HttpResponseData> DeleteCustomer(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "customers/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation($"Deleting customer with ID: {id}");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient("customers");
                await tableClient.DeleteEntityAsync("customers", id);

                var response = req.CreateResponse(HttpStatusCode.NoContent);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting customer {id}");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }
    }
}

