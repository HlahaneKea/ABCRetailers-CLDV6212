using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class FileUploadModel
    {
        [Required(ErrorMessage = "Proof of payment file is required")]
        public IFormFile? ProofOfPayment { get; set; }

        public string? OrderId { get; set; }
        public string? CustomerName { get; set; }
    }
} 