using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class City
{
    public int CityId { get; set; }

    public int CountryId { get; set; }

    public string CityName { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();

    public virtual Country Country { get; set; } = null!;
}
