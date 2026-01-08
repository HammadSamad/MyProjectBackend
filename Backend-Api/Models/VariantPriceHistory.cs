using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class VariantPriceHistory
{
    public long Id { get; set; }

    public int VariantId { get; set; }

    public decimal? OldPrice { get; set; }

    public decimal? NewPrice { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ProductVariant Variant { get; set; } = null!;
}
