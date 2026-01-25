using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class OrderItem
{
    public long OrderItemId { get; set; }

    public long OrderId { get; set; }

    public int VariantId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
