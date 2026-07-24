using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TourAPI.Repositories;
using TourAPI.Services;

namespace TourAPI.BackgroundServices
{
    public class ScheduleNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduleNotificationBackgroundService> _logger;

        public ScheduleNotificationBackgroundService(IServiceProvider serviceProvider, ILogger<ScheduleNotificationBackgroundService> logger)
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
                    var now = DateTime.Now;
                    var nextRunTime = now.Date.AddHours(8); // 8:00 AM
                    
                    if (now > nextRunTime)
                    {
                        nextRunTime = nextRunTime.AddDays(1);
                    }
                    
                    var delay = nextRunTime - now;
                    // _logger.LogInformation($"Next schedule notification will run at {nextRunTime}. Waiting for {delay}");
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
                    _logger.LogError(ex, "Error occurred executing ScheduleNotificationBackgroundService.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry after 5 mins if error
                }
            }
        }

        private async Task ProcessNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var _scheduleRepo = scope.ServiceProvider.GetRequiredService<ITourScheduleRepository>();
            var _notificationService = scope.ServiceProvider.GetRequiredService<INotificationInternalService>();

            var tomorrow = DateTime.Now.AddDays(1).Date;

            // Find schedules starting tomorrow
            var schedulesStartingTomorrow = await _scheduleRepo.GetSchedulesStartingOnAsync(tomorrow);

            foreach (var schedule in schedulesStartingTomorrow)
            {
                var tourName = schedule.Tour?.Name ?? "Tour";
                var titleManager = "Tour Starting Soon";
                var contentManager = $"Your tour schedule '{tourName}' is starting tomorrow ({tomorrow:dd/MM/yyyy}).";

                var titleStaff = "Tour Assignment Tomorrow";
                var contentStaff = $"You are assigned to the tour '{tourName}' which starts tomorrow ({tomorrow:dd/MM/yyyy}).";

                // Notify Manager
                if (schedule.Tour != null && schedule.Tour.CreatedBy > 0)
                {
                    await _notificationService.NotifyUserAsync(schedule.Tour.CreatedBy, titleManager, contentManager);
                }

                // Notify Staffs
                if (schedule.TourScheduleStaffs != null)
                {
                    foreach (var staff in schedule.TourScheduleStaffs)
                    {
                        await _notificationService.NotifyUserAsync(staff.StaffId, titleStaff, contentStaff);
                    }
                }
            }
        }
    }
}
