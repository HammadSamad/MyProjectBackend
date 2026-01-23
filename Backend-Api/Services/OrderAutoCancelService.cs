using Backend_Api.Data;
using Microsoft.EntityFrameworkCore;

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
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<LaptopHarbourDbContext>();

                var cutoffTime = DateTime.UtcNow.AddMinutes(-30);

                var pendingOrders = await db.Orders
                    .Where(o => o.OrderStatus == "Pending" && o.CreatedAt <= cutoffTime)
                    .ToListAsync();

                if (pendingOrders.Any())
                {
                    foreach (var order in pendingOrders)
                    {
                        order.OrderStatus = "Cancelled";
                        order.UpdatedAt = DateTime.UtcNow;
                    }

                    await db.SaveChangesAsync();
                }
            }

            // Run every 5 minutes
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
