namespace ABCRetailers.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int CartId { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}

