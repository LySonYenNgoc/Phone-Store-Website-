using System;
using System.Collections.Generic;

namespace PhoneStore.Models;

public partial class Color
{
    public int ColorId { get; set; }

    public string? ColorName { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
}
