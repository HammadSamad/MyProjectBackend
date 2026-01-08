using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Wishlist
{
    public int WishlistId { get; set; }

    public int? UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }

    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
