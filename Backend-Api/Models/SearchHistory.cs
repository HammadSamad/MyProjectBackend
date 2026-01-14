using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class SearchHistory
{
    public long SearchId { get; set; }

    public int? UserId { get; set; }

    public string? SearchText { get; set; }

    public DateTime? SearchedAt { get; set; }

    public virtual User? User { get; set; }
}
