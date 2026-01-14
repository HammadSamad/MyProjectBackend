using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Payment
{
    public long PaymentId { get; set; }

    public long OrderId { get; set; }

    public int UserId { get; set; }

    public int PaymentMethodId { get; set; }

    public decimal Amount { get; set; }

    public string? Status { get; set; }

    public string? TransactionReference { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual User User { get; set; } = null!;
}
