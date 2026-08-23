using System.ComponentModel.DataAnnotations;

namespace PhoneStore.Customer.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal Total { get; set; }
        public List<ShippingAddressViewModel> Addresses { get; set; } = new();
        public List<string> PaymentMethods { get; set; } = new();
        public int? SelectedAddressId { get; set; }
        public string? SelectedPaymentMethod { get; set; }
        public string? Notes { get; set; }
        public bool CreateNewAddress { get; set; }
        public ShippingAddressViewModel? NewAddress { get; set; }
    }

    public class ShippingAddressViewModel
    {
        public int AddressId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được quá 100 ký tự")]
        public string RecipientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(15, ErrorMessage = "Số điện thoại không được quá 15 ký tự")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được quá 500 ký tự")]
        public string AddressLine { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập phường/xã")]
        [StringLength(100, ErrorMessage = "Phường/xã không được quá 100 ký tự")]
        public string Ward { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập quận/huyện")]
        [StringLength(100, ErrorMessage = "Quận/huyện không được quá 100 ký tự")]
        public string District { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tỉnh/thành phố")]
        [StringLength(100, ErrorMessage = "Tỉnh/thành phố không được quá 100 ký tự")]
        public string Province { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public string FullAddress => $"{AddressLine}, {Ward}, {District}, {Province}";
    }

    public class OrderConfirmationViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber => $"#{OrderId:D6}";
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal TotalAmount { get; set; }

        public List<CartItemViewModel> Items { get; set; } = new();
        public ShippingAddressViewModel? ShippingAddress { get; set; }
        public CustomerViewModel? Customer { get; set; }
    }

    public class CustomerViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
