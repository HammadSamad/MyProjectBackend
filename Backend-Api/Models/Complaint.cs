using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Complaint
{
    public long ComplaintId { get; set; }

    public int UserId { get; set; }

    public long? OrderId { get; set; }

    public string? Subject { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public string? Priority { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ComplaintMessage> ComplaintMessages { get; set; } = new List<ComplaintMessage>();

    public virtual Order? Order { get; set; }

    public virtual User User { get; set; } = null!;
}
