using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Shipment
{
    public long ShipmentId { get; set; }

    public long OrderId { get; set; }

    public string? TrackingNumber { get; set; }

    public string? CourierName { get; set; }

    public decimal ShippingCost { get; set; }

    public string? Status { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}
