using System;
using System.Collections.Generic;

namespace PhoneStore.Models;

public partial class ShippingAddress
{
    public int AddressId { get; set; }

    public int? CustomerId { get; set; }

    public string? RecipientName { get; set; }

    public string? Phone { get; set; }

    public string? AddressLine { get; set; }

    public string? Ward { get; set; }

    public string? District { get; set; }

    public string? Province { get; set; }

    public bool? IsDefault { get; set; }

    public virtual Customer? Customer { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
