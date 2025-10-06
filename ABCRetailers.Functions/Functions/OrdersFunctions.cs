using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Helpers;
using ABCRetailers.Functions.Models;
using System.Net;
using System.Text.Json;

namespace ABCRetailers.Functions.Functions
{
    public class OrdersFunctions
    {
        private readonly ILogger _logger;
        private readonly TableServiceClient _tableServiceClient;
        private readonly QueueServiceClient _queueServiceClient;

        public OrdersFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrdersFunctions>();
            
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection") 
                ?? throw new InvalidOperationException("AzureStorageConnection is not configured");
            
            _tableServiceClient = new TableServiceClient(connectionString);
            _queueServiceClient = new QueueServiceClient(connectionString);
        }

        [Function("GetOrders")]
        public async Task<HttpResponseData> GetOrders(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequestData req)
        {
            _logger.LogInformation("Getting all orders");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient("orders");
                await tableClient.CreateIfNotExistsAsync();

                var orders = new List<OrderDto>();
                await foreach (var order in tableClient.QueryAsync<OrderEntity>())
                {
                    orders.Add(order.ToDto());
                }

                return await HttpJson.WriteJsonAsync(req, orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("GetOrder")]
        public async Task<HttpResponseData> GetOrder(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation($"Getting order with ID: {id}");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient("orders");
                var response = await tableClient.GetEntityAsync<OrderEntity>("orders", id);
                
                return await HttpJson.WriteJsonAsync(req, response.Value.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving order {id}");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.NotFound);
            }
        }

        // HTTP trigger to write order to queue (not directly to table)
        [Function("CreateOrder")]
        public async Task<HttpResponseData> CreateOrder(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req)
        {
            _logger.LogInformation("Submitting new order to queue");

            try
            {
                var orderMessage = await HttpJson.ReadJsonAsync<OrderQueueMessage>(req);
                if (orderMessage == null)
                {
                    return await HttpJson.WriteErrorAsync(req, "Invalid order data");
                }

                // Send to queue instead of directly to table
                var queueClient = _queueServiceClient.GetQueueClient("order-processing");
                await queueClient.CreateIfNotExistsAsync();

                var messageJson = JsonSerializer.Serialize(orderMessage);
                await queueClient.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageJson)));

                _logger.LogInformation("Order submitted to queue successfully");

                var response = req.CreateResponse(HttpStatusCode.Accepted);
                await response.WriteAsJsonAsync(new { message = "Order submitted for processing", status = "queued" });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting order to queue");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("UpdateOrder")]
        public async Task<HttpResponseData> UpdateOrder(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "orders/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation($"Updating order with ID: {id}");

            try
            {
                var orderDto = await HttpJson.ReadJsonAsync<OrderDto>(req);
                if (orderDto == null)
                {
                    return await HttpJson.WriteErrorAsync(req, "Invalid order data");
                }

                orderDto.RowKey = id;
                orderDto.PartitionKey = "orders";

                var entity = orderDto.ToEntity();
                
                var tableClient = _tableServiceClient.GetTableClient("orders");
                await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);

                return await HttpJson.WriteJsonAsync(req, entity.ToDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order {id}");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        [Function("DeleteOrder")]
        public async Task<HttpResponseData> DeleteOrder(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "orders/{id}")] HttpRequestData req,
            string id)
        {
            _logger.LogInformation($"Deleting order with ID: {id}");

            try
            {
                var tableClient = _tableServiceClient.GetTableClient("orders");
                await tableClient.DeleteEntityAsync("orders", id);

                var response = req.CreateResponse(HttpStatusCode.NoContent);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting order {id}");
                return await HttpJson.WriteErrorAsync(req, ex.Message, HttpStatusCode.InternalServerError);
            }
        }
    }
}

