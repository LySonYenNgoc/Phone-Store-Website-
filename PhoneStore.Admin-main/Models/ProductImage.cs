using System;
using System.Collections.Generic;

namespace PhoneStore.Models;

public partial class ProductImage
{
    public int ImageId { get; set; }

    public int? ProductId { get; set; }

    public int? ColorId { get; set; }    public byte[] ImageData { get; set; } = null!;

    public string ImageMimeType { get; set; } = null!;

    public virtual Color? Color { get; set; }

    public virtual Product? Product { get; set; }
}
