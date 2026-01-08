using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class SpecificationDefinition
{
    public int SpecificationId { get; set; }

    public string? SpecificationName { get; set; }

    public string? DataType { get; set; }

    public bool? IsVariant { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<ProductSpecificationValue> ProductSpecificationValues { get; set; } = new List<ProductSpecificationValue>();

    public virtual ICollection<SpecificationOption> SpecificationOptions { get; set; } = new List<SpecificationOption>();
}
