using System;
using System.Collections.Generic;

namespace PhoneStore.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public int? BirthYear { get; set; }

    public string? PasswordHash { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int? MembershipId { get; set; }

    public virtual Membership? Membership { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<ShippingAddress> ShippingAddresses { get; set; } = new List<ShippingAddress>();
}
