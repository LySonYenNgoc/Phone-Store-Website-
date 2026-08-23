using System;
using System.Collections.Generic;

namespace PhoneStore.Models;

public partial class DiscountProgram
{
    public int DiscountId { get; set; }

    public string? DiscountName { get; set; }

    public int? DiscountPercent { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
