namespace ABCRetailers.Functions.Models
{
    // DTOs for API communication (camelCase for JSON)
    public class CustomerDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
    }

    public class ProductDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Price { get; set; }
        public int StockAvailable { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
    }

    public class OrderDto
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Submitted";
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
    }

    public class OrderQueueMessage
    {
        public string CustomerId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
        public string Status { get; set; } = "Submitted";
    }

    public class BlobUploadResponse
    {
        public string BlobUrl { get; set; } = string.Empty;
        public string BlobName { get; set; } = string.Empty;
    }

    public class FileUploadResponse
    {
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }
}

