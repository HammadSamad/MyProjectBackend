using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class UserRecentOrder
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public long OrderId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
