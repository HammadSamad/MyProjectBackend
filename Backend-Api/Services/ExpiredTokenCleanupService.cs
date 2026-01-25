using Backend_Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backend_Api.Services
{
    public class ExpiredTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ExpiredTokenCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        public ExpiredTokenCleanupService(IServiceScopeFactory scopeFactory, ILogger<ExpiredTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExpiredTokenCleanupService started.");

            // Use PeriodicTimer for clean periodic execution
            using var timer = new PeriodicTimer(_cleanupInterval);

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var _context = scope.ServiceProvider.GetRequiredService<LaptopHarbourDbContext>();

                        // Cleanup expired OTPs
                        bool hasExpiredOtps = await _context.UserVerifications
                            .AnyAsync(v => v.ExpiresAt < DateTime.UtcNow && v.IsUsed != true, stoppingToken);

                        if (hasExpiredOtps)
                        {
                            var expiredOtps = await _context.UserVerifications
                                .Where(v => v.ExpiresAt < DateTime.UtcNow && v.IsUsed != true)
                                .ToListAsync(stoppingToken);

                            _context.UserVerifications.RemoveRange(expiredOtps);
                            _logger.LogInformation($"Deleted {expiredOtps.Count} expired OTP(s).");
                        }

                        // Cleanup expired password reset tokens
                        bool hasExpiredResetTokens = await _context.PasswordResetTokens
                            .AnyAsync(t => t.ExpiresAt < DateTime.UtcNow && t.IsUsed != true, stoppingToken);

                        if (hasExpiredResetTokens)
                        {
                            var expiredResetTokens = await _context.PasswordResetTokens
                                .Where(t => t.ExpiresAt < DateTime.UtcNow && t.IsUsed != true)
                                .ToListAsync(stoppingToken);

                            _context.PasswordResetTokens.RemoveRange(expiredResetTokens);
                            _logger.LogInformation($"Deleted {expiredResetTokens.Count} expired password reset token(s).");
                        }

                        // Save changes only if any deletions occurred
                        if (hasExpiredOtps || hasExpiredResetTokens)
                        {
                            await _context.SaveChangesAsync(stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during expired token cleanup.");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Timer was canceled due to stoppingToken, graceful exit
            }

            _logger.LogInformation("ExpiredTokenCleanupService stopped.");
        }
    }
}
