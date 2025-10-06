using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Models;
using System.Text.Json;

namespace ABCRetailers.Functions.Functions
{
    public class QueueProcessorFunctions
    {
        private readonly ILogger _logger;
        private readonly TableServiceClient _tableServiceClient;

        public QueueProcessorFunctions(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<QueueProcessorFunctions>();
            
            var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection") 
                ?? throw new InvalidOperationException("AzureStorageConnection is not configured");
            
            _tableServiceClient = new TableServiceClient(connectionString);
        }

        /// <summary>
        /// Queue trigger function that processes orders from the queue and stores them in table storage
        /// </summary>
        [Function("ProcessOrderQueue")]
        public async Task ProcessOrderQueue(
            [QueueTrigger("order-processing", Connection = "AzureStorageConnection")] string queueMessage)
        {
            _logger.LogInformation($"Processing order from queue: {queueMessage}");

            try
            {
                // Deserialize the queue message
                var orderMessage = JsonSerializer.Deserialize<OrderQueueMessage>(queueMessage);
                if (orderMessage == null)
                {
                    _logger.LogError("Failed to deserialize order message");
                    return;
                }

                // Generate unique order ID
                string orderId = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{Guid.NewGuid():N}";

                // Create order entity
                var orderEntity = new OrderEntity
                {
                    PartitionKey = "orders",
                    RowKey = orderId,
                    CustomerId = orderMessage.CustomerId,
                    Username = orderMessage.Username,
                    ProductId = orderMessage.ProductId,
                    ProductName = orderMessage.ProductName,
                    OrderDate = orderMessage.OrderDate,
                    Quantity = orderMessage.Quantity,
                    UnitPrice = orderMessage.UnitPrice,
                    TotalPrice = orderMessage.TotalPrice,
                    Status = orderMessage.Status
                };

                // Store in table storage
                var tableClient = _tableServiceClient.GetTableClient("orders");
                await tableClient.CreateIfNotExistsAsync();
                await tableClient.AddEntityAsync(orderEntity);

                _logger.LogInformation($"Order {orderId} successfully processed and stored in table");

                // Send notification to order-notifications queue
                await SendOrderNotificationAsync(orderEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order from queue");
                throw; // Re-throw to allow Azure to handle retry logic
            }
        }

        /// <summary>
        /// Sends order notification to the notification queue
        /// </summary>
        private async Task SendOrderNotificationAsync(OrderEntity order)
        {
            try
            {
                var connectionString = Environment.GetEnvironmentVariable("AzureStorageConnection");
                var queueServiceClient = new Azure.Storage.Queues.QueueServiceClient(connectionString);
                var queueClient = queueServiceClient.GetQueueClient("order-notifications");
                await queueClient.CreateIfNotExistsAsync();

                var notification = new
                {
                    order.OrderId,
                    order.CustomerId,
                    CustomerName = order.Username,
                    order.ProductName,
                    order.Quantity,
                    order.TotalPrice,
                    order.OrderDate,
                    order.Status,
                    ProcessedAt = DateTime.UtcNow
                };

                var messageJson = JsonSerializer.Serialize(notification);
                await queueClient.SendMessageAsync(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(messageJson)));

                _logger.LogInformation($"Notification sent for order {order.OrderId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order notification");
                // Don't throw - notification failure shouldn't fail the main operation
            }
        }
    }
}

