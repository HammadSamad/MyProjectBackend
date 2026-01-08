using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class ProductVariant
{
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    public string? Sku { get; set; }

    public decimal? Price { get; set; }

    public int? Stock { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<VariantPriceHistory> VariantPriceHistories { get; set; } = new List<VariantPriceHistory>();

    public virtual ICollection<VariantSpecificationOption> VariantSpecificationOptions { get; set; } = new List<VariantSpecificationOption>();

    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
