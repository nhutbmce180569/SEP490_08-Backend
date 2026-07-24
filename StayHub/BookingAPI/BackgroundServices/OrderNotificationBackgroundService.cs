using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using BookingAPI.DTOs;
using BookingAPI.Repositories;
using BookingAPI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingAPI.BackgroundServices
{
    public class OrderNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderNotificationBackgroundService> _logger;

        public OrderNotificationBackgroundService(IServiceProvider serviceProvider, ILogger<OrderNotificationBackgroundService> logger)
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
                    // _logger.LogInformation($"Next order notification will run at {nextRunTime}. Waiting for {delay}");
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
                    _logger.LogError(ex, "Error occurred executing OrderNotificationBackgroundService.");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry after 5 mins if error
                }
            }
        }

        private async Task ProcessNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var _orderRepo = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var _notificationService = scope.ServiceProvider.GetRequiredService<INotificationInternalService>();
            var _httpClientFactory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            
            var tomorrow = DateTime.Now.AddDays(1).Date;

            try
            {
                var client = _httpClientFactory.CreateClient("TourApiClient");
                var response = await client.GetAsync("api/tourschedules/internal/starting-tomorrow");

                if (response.IsSuccessStatusCode)
                {
                    var schedules = await response.Content.ReadFromJsonAsync<System.Collections.Generic.List<ReadOrderScheduleDTO>>();
                    if (schedules != null && schedules.Any())
                    {
                        foreach (var schedule in schedules)
                        {
                            var orders = await _orderRepo.GetByScheduleIdAsync(schedule.Id);
                            var paidOrders = orders.Where(o => o.Status == "Paid").ToList();

                            foreach (var order in paidOrders)
                            {
                                var title = "Upcoming Tour";
                                var content = $"Your tour will depart tomorrow ({tomorrow:dd/MM/yyyy}). Please prepare and be on time!";
                                
                                await _notificationService.NotifyUserAsync(order.CustomerId, title, content);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get schedules starting tomorrow or send customer notifications.");
            }
        }
    }
}
