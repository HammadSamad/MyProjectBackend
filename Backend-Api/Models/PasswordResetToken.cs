using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class PasswordResetToken
{
    public long ResetId { get; set; }

    public int UserId { get; set; }

    public string ResetToken { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool? IsUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
