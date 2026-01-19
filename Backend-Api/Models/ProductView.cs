using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class ProductView
{
    public long ViewId { get; set; }

    public int? UserId { get; set; }

    public int ProductId { get; set; }

    public DateTime? ViewedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User? User { get; set; }
}
