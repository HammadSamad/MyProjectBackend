using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class OrderAddress
{
    public long OrderAddressId { get; set; }

    public long OrderId { get; set; }

    public string? RecipientName { get; set; }

    public int AddressId { get; set; }

    public string? Phone { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Address Address { get; set; } = null!;

    public virtual Order Order { get; set; } = null!;
}
