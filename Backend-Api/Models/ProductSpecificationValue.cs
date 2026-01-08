using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class ProductSpecificationValue
{
    public int ProductId { get; set; }

    public int SpecificationId { get; set; }

    public string? ValueText { get; set; }

    public decimal? ValueNumber { get; set; }

    public bool? ValueBool { get; set; }

    public int? OptionId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual SpecificationOption? Option { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual SpecificationDefinition Specification { get; set; } = null!;
}
