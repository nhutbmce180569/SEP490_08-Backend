using Microsoft.AspNetCore.Mvc;
using TourAPI.DTOs;

namespace TourAPI.Services
{
    public interface IOrderService
    {
        Task<bool> CheckTourHasOrder(CheckBookingTour checkBookingTour);
        Task<bool> CheckCompletedOrder(CheckCompletedBookingRequest request);

    }
}
