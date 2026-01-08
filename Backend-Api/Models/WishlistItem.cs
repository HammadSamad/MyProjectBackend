using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class WishlistItem
{
    public int WishlistItemId { get; set; }

    public int WishlistId { get; set; }

    public int VariantId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ProductVariant Variant { get; set; } = null!;

    public virtual Wishlist Wishlist { get; set; } = null!;
}
