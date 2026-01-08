using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class SpecificationOption
{
    public int OptionId { get; set; }

    public int SpecificationId { get; set; }

    public string? OptionValue { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ProductSpecificationValue> ProductSpecificationValues { get; set; } = new List<ProductSpecificationValue>();

    public virtual SpecificationDefinition Specification { get; set; } = null!;

    public virtual ICollection<VariantSpecificationOption> VariantSpecificationOptions { get; set; } = new List<VariantSpecificationOption>();
}
