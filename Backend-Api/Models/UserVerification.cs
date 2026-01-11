using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class UserVerification
{
    public long VerificationId { get; set; }

    public int UserId { get; set; }

    public string? Channel { get; set; }

    public string? Code { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public bool? IsUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
