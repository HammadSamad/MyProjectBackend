using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Refund
{
    public long RefundId { get; set; }

    public long PaymentId { get; set; }

    public long? ReturnId { get; set; }

    public decimal Amount { get; set; }

    public string? Reason { get; set; }

    public string? Status { get; set; }

    public DateTime? RefundedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Payment Payment { get; set; } = null!;

    public virtual Return? Return { get; set; }
}
