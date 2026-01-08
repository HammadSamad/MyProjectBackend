using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Address
{
    public int AddressId { get; set; }

    public int UserId { get; set; }

    public int CityId { get; set; }

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? PostalCode { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual City City { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
