
using Microsoft.AspNetCore.Mvc;
using TourAPI.DTOs;

namespace TourAPI.Services.Implements
{
    public class OrderService : IOrderService
    {
        private readonly HttpClient _httpClient;
        public OrderService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> CheckCompletedOrder(CheckCompletedBookingRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/orders/check-completed-booking",
                request
            );

            if (!response.IsSuccessStatusCode)
                throw new Exception(
                    "Failed to verify booking information.");

            return await response.Content.ReadFromJsonAsync<bool>();
        }

        public async Task<bool> CheckTourHasOrder(CheckBookingTour checkBookingTour)
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/orders/check-booking",
                checkBookingTour
            );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<bool>();
        }
    }
}
