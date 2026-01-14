using System;
using System.Collections.Generic;

namespace Backend_Api.Models;

public partial class Order
{
    public long OrderId { get; set; }

    public int UserId { get; set; }

    public decimal? TotalAmount { get; set; }

    public int? PaymentMethodId { get; set; }

    public string? OrderStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual ICollection<OrderAddress> OrderAddresses { get; set; } = new List<OrderAddress>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual PaymentMethod? PaymentMethod { get; set; }

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Return> Returns { get; set; } = new List<Return>();

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserRecentOrder> UserRecentOrders { get; set; } = new List<UserRecentOrder>();
}
