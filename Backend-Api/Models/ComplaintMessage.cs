using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class ComplaintMessage
{
    public long MessageId { get; set; }

    public long ComplaintId { get; set; }

    public string? SenderType { get; set; }

    public string? Message { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Complaint Complaint { get; set; } = null!;
}
