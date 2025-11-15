using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required(ErrorMessage = "Customer is required")]
        public string CustomerId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Product is required")]
        public string ProductId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";

        // Dropdown data
        public List<Customer> Customers { get; set; } = new List<Customer>();
        public List<Product> Products { get; set; } = new List<Product>();
    }

    public class UpdateOrderStatusRequest
    {
        public string Id { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
    }
}