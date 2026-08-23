using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStore.Customer.Models
{
    [Table("ProductImages")]
    public class ProductImage
    {
        [Key]
        public int ImageId { get; set; }

        public int? ProductId { get; set; }

        public int? ColorId { get; set; }

        [Required]
        public byte[] ImageData { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string ImageMimeType { get; set; } = null!;        // Helper method to get base64 data URL
        public string ImageUrl => $"data:{ImageMimeType};base64,{Convert.ToBase64String(ImageData)}";

        // Navigation properties
        public virtual Product? Product { get; set; }
        public virtual Color? Color { get; set; }
    }
}
