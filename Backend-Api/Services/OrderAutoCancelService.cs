using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class OrderAutoCancelService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderAutoCancelService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<LaptopHarbourDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var cutoffTime = DateTime.UtcNow.AddMinutes(-30);

                    var pendingOrders = await db.Orders
                        .Include(o => o.User)
                        .Where(o => o.OrderStatus == "Pending" && o.CreatedAt <= cutoffTime)
                        .ToListAsync(stoppingToken);

                    if (pendingOrders.Any())
                    {
                        foreach (var order in pendingOrders)
                        {
                            order.OrderStatus = "Cancelled";
                            order.UpdatedAt = DateTime.UtcNow;

                            // ---------------- Notify User ----------------
                            db.Notifications.Add(new Notification
                            {
                                UserId = order.UserId,
                                Title = "Order Cancelled",
                                Message = $"Your order #{order.OrderId} has been automatically cancelled due to inactivity.",
                                Type = "OrderAutoCancel",
                                TargetAudience = "User",
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow
                            });

                            // Send Email to User
                            if (order.User != null && !string.IsNullOrWhiteSpace(order.User.Email))
                            {
                                string body = $"<h3>Your order #{order.OrderId} has been cancelled</h3>" +
                                              "<p>This order was automatically cancelled because it was not completed within 30 minutes.</p>" +
                                              "<p>If you have any questions, please contact support.</p>";

                                await emailService.SendEmailAsync(order.User.Email, "Order Cancelled", body);
                            }

                            // ---------------- Notify Admin ----------------
                            var adminUsers = await db.UserRoles
                                .Where(ur => ur.Role.RoleName == "Admin")
                                .Select(ur => ur.User)
                                .Distinct()
                                .ToListAsync(stoppingToken);

                            foreach (var admin in adminUsers)
                            {
                                db.Notifications.Add(new Notification
                                {
                                    UserId = admin.UserId,
                                    Title = "Order Auto-Cancelled",
                                    Message = $"Order #{order.OrderId} has been automatically cancelled.",
                                    Type = "OrderAutoCancel",
                                    TargetAudience = "Admin",
                                    IsRead = false,
                                    CreatedAt = DateTime.UtcNow
                                });
                            }
                        }

                        await db.SaveChangesAsync(stoppingToken);
                    }
                }

                // Wait 5 minutes before next check
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OrderAutoCancelService error: {ex.Message}");
            }
        }
    }
}
