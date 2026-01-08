using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class ProductImage
{
    public int ImageId { get; set; }

    public int ProductId { get; set; }

    public string? ImageUrl { get; set; }

    public bool? IsCover { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
