using ABCRetailers.Functions.Entities;
using ABCRetailers.Functions.Models;

namespace ABCRetailers.Functions.Helpers
{
    public static class Map
    {
        // Customer mapping
        public static CustomerEntity ToEntity(this CustomerDto dto)
        {
            return new CustomerEntity
            {
                PartitionKey = "customers",
                RowKey = dto.CustomerId,
                CustomerId = dto.CustomerId,
                Name = dto.Name,
                Surname = dto.Surname,
                Email = dto.Email,
                ContactNumber = dto.ContactNumber,
                Username = dto.Username
            };
        }

        public static CustomerDto ToDto(this CustomerEntity entity)
        {
            return new CustomerDto
            {
                CustomerId = entity.CustomerId,
                Name = entity.Name,
                Surname = entity.Surname,
                Email = entity.Email,
                ContactNumber = entity.ContactNumber,
                Username = entity.Username,
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey
            };
        }

        // Product mapping
        public static ProductEntity ToEntity(this ProductDto dto)
        {
            return new ProductEntity
            {
                PartitionKey = "products",
                RowKey = dto.ProductId,
                ProductId = dto.ProductId,
                ProductName = dto.ProductName,
                Description = dto.Description,
                Price = dto.Price,
                StockAvailable = dto.StockAvailable,
                ImageUrl = dto.ImageUrl
            };
        }

        public static ProductDto ToDto(this ProductEntity entity)
        {
            return new ProductDto
            {
                ProductId = entity.ProductId,
                ProductName = entity.ProductName,
                Description = entity.Description,
                Price = entity.Price,
                StockAvailable = entity.StockAvailable,
                ImageUrl = entity.ImageUrl,
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey
            };
        }

        // Order mapping
        public static OrderEntity ToEntity(this OrderDto dto)
        {
            return new OrderEntity
            {
                PartitionKey = "orders",
                RowKey = dto.OrderId,
                CustomerId = dto.CustomerId,
                Username = dto.Username,
                ProductId = dto.ProductId,
                ProductName = dto.ProductName,
                OrderDate = dto.OrderDate,
                Quantity = dto.Quantity,
                UnitPrice = dto.UnitPrice,
                TotalPrice = dto.TotalPrice,
                Status = dto.Status
            };
        }

        public static OrderDto ToDto(this OrderEntity entity)
        {
            return new OrderDto
            {
                OrderId = entity.OrderId,
                CustomerId = entity.CustomerId,
                Username = entity.Username,
                ProductId = entity.ProductId,
                ProductName = entity.ProductName,
                OrderDate = entity.OrderDate,
                Quantity = entity.Quantity,
                UnitPrice = entity.UnitPrice,
                TotalPrice = entity.TotalPrice,
                Status = entity.Status,
                PartitionKey = entity.PartitionKey,
                RowKey = entity.RowKey
            };
        }
    }
}

