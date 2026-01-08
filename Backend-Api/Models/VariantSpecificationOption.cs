using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class VariantSpecificationOption
{
    public int VariantId { get; set; }

    public int OptionId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual SpecificationOption Option { get; set; } = null!;

    public virtual ProductVariant Variant { get; set; } = null!;
}
