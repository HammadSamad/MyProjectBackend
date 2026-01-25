using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Country
{
    public int CountryId { get; set; }

    public string? CountryName { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<City> Cities { get; set; } = new List<City>();
}
