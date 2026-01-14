using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Return
{
    public long ReturnId { get; set; }

    public long OrderId { get; set; }

    public int UserId { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual User User { get; set; } = null!;
}
