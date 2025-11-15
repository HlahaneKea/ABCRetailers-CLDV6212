using Azure.Data.Tables;
using Azure;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Product : ITableEntity
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public string PriceString { get; set; } = string.Empty;
        public int StockAvailable { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        // ITableEntity implementation
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}