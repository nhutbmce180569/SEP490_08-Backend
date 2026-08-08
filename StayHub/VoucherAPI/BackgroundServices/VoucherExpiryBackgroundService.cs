using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoucherAPI.Models;
using VoucherAPI.Services;

namespace VoucherAPI.BackgroundServices
{
    public class VoucherExpiryBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VoucherExpiryBackgroundService> _logger;

        public VoucherExpiryBackgroundService(IServiceProvider serviceProvider, ILogger<VoucherExpiryBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await ProcessNotificationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Initial execution failed.");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Calculate delay until next 8:00 AM
                    var now = DateTime.UtcNow;
                    var nextRunTime = now.Date.AddHours(1); // 8:00 AM UTC+7 is 1:00 AM UTC
                    
                    if (now > nextRunTime)
                    {
                        nextRunTime = nextRunTime.AddDays(1);
                    }
                    
                    var delay = nextRunTime - now;
                    // _logger.LogInformation($"Next voucher expiry notification will run at {nextRunTime}. Waiting for {delay}");
                    // Uncomment below to test every 1 minute
                    // delay = TimeSpan.FromMinutes(1);

                    await Task.Delay(delay, stoppingToken);

                    await ProcessNotificationsAsync();
                }
                catch (TaskCanceledException)
                {
                    // Ignore
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing VoucherExpiryBackgroundService.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry after 5 mins if error
                }
            }
        }

        private async Task ProcessNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var _dbContext = scope.ServiceProvider.GetRequiredService<StayHubVoucherDbContext>();
            var _notificationService = scope.ServiceProvider.GetRequiredService<INotificationInternalService>();
            var _emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var _userValidationService = scope.ServiceProvider.GetRequiredService<IUserValidationService>();

            var tomorrow = DateTime.UtcNow.AddDays(1).Date;
            var dayAfterTomorrow = tomorrow.AddDays(1);

            try
            {
                // Find active vouchers that end tomorrow
                var expiringVouchers = await _dbContext.Vouchers
                    .Where(v => v.IsActive && v.EndDate >= tomorrow && v.EndDate < dayAfterTomorrow)
                    .ToListAsync();

                foreach (var voucher in expiringVouchers)
                {
                    if (voucher.CreatorId > 0)
                    {
                        // [FIX NOTIFY] Send in-app notification (existing)
                        var title = "Voucher Expiring Soon";
                        var content = $"Your voucher code '{voucher.Code}' will expire tomorrow ({voucher.EndDate:dd/MM/yyyy}). Please review if you want to extend it.";
                        await _notificationService.NotifyUserAsync(voucher.CreatorId, title, content);

                        // [FIX NOTIFY] Also send email to the Manager/Creator — consistent with birthday voucher flow
                        try
                        {
                            var creatorInfo = await _userValidationService.ValidateUserAsync(voucher.CreatorId);
                            if (creatorInfo.Exists && !string.IsNullOrWhiteSpace(creatorInfo.Email))
                            {
                                var emailSubject = $"[StayHub] Voucher '{voucher.Code}' sắp hết hạn vào ngày mai";
                                var emailBody = $@"
                                    <h3>Voucher sắp hết hạn</h3>
                                    <p>Xin chào {creatorInfo.FullName},</p>
                                    <p>Voucher của bạn sẽ hết hạn vào <strong>ngày mai ({voucher.EndDate:dd/MM/yyyy})</strong>:</p>
                                    <ul>
                                        <li><strong>Mã voucher:</strong> {voucher.Code}</li>
                                        <li><strong>Đã dùng:</strong> {voucher.UsedCount} / {voucher.AvailableCount}</li>
                                        <li><strong>Ngày hết hạn:</strong> {voucher.EndDate:dd/MM/yyyy HH:mm} (UTC)</li>
                                    </ul>
                                    <p>Nếu muốn gia hạn, vui lòng cập nhật EndDate trước khi voucher hết hiệu lực.</p>";
                                await _emailService.SendEmailAsync(creatorInfo.Email, emailSubject, emailBody);
                            }
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogWarning(emailEx, "Failed to send expiry email for voucher {Code} to creator {CreatorId}", voucher.Code, voucher.CreatorId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get expiring vouchers or send manager notifications.");
            }
        }
    }
}
