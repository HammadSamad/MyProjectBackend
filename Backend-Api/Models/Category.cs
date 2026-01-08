using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public int? ParentCategoryId { get; set; }

    public string? CategoryName { get; set; }

    public string? CategoryImage { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Category> InverseParentCategory { get; set; } = new List<Category>();

    public virtual Category? ParentCategory { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
