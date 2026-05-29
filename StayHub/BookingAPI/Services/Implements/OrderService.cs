using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Exceptions;
using BookingAPI.Models;
using BookingAPI.Repositories;
using BookingAPI.Services;
using System;
using System.Linq;

namespace BookingAPI.Services.Implements
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IMapper _mapper;
        private readonly ITourApiClient _tourApiClient;
        private readonly IBackgroundJobService _backgroundJobService;
        public OrderService(
         IOrderRepository orderRepository,
         IMapper mapper,
         ITourApiClient tourApiClient,
         IBackgroundJobService backgroundJobService)
        {
            _orderRepository = orderRepository;
            _mapper = mapper;
            _tourApiClient = tourApiClient;
            _backgroundJobService = backgroundJobService;
        }

        public async Task<bool> CheckCompletedBookingAsync(CheckBookingRequest request)
        {
            if (request == null || request.ScheduleIds == null || !request.ScheduleIds.Any())
            {
                return false;
            }

            return await _orderRepository.HasCompletedBookingAsync(request.CustomerId, request.ScheduleIds);
        }
        public async Task<bool> CheckBookingAsync(CheckBookingTour request)
        {
            if (request == null || request.ScheduleIds == null || !request.ScheduleIds.Any())
            {
                return false;
            }

            return await _orderRepository.HasBookingAsync(request.ScheduleIds);
        }
        public async Task<ReadOrderDTO> CreateOrderAsync(int customerId, CreateOrderDTO request)
        {
            if (request.Tickets == null || !request.Tickets.Any())
            {
                throw new BookingValidationException("At least one ticket is required.");
            }

            var ticketCount = request.Tickets.Count;
            await ValidateScheduleForBookingAsync(request.ScheduleId, ticketCount);

            var order = _mapper.Map<Order>(request);

            // Thiết lập giá trị mặc định cho đơn hàng
            order.CustomerId = customerId;
            order.TicketCount = ticketCount;
            order.Status = "Pending";
            order.InviteToken = Guid.NewGuid().ToString();
            order.OrderedAt = DateTime.Now;
            // Thiết lập giá trị mặc định cho từng vé
            foreach (var ticket in order.Tickets)
            {
                ticket.CheckInStatus = "Pending";
                ticket.QrCode = Guid.NewGuid().ToString(); // Tạo mã QR ngẫu nhiên
            }

            var savedOrder = await _orderRepository.AddAsync(order);

            var seatsReserved = await _tourApiClient.ReserveScheduleSeatsAsync(request.ScheduleId, ticketCount);
            if (!seatsReserved)
            {
                throw new BookingValidationException("Could not reserve seats for this departure. Please try again.");
            }

            _backgroundJobService.ScheduleAutoCancelOrder(savedOrder.Id);
            var dto = _mapper.Map<ReadOrderDTO>(savedOrder);
            await EnrichOrderDtoAsync(dto);
            return dto;
        }

        public async Task<ReadOrderDTO?> GetOrderByIdAsync(int id, int customerId)
        {
            var order = await _orderRepository.GetByIdAndCustomerIdAsync(id, customerId);
            if (order == null) return null;

            var dto = _mapper.Map<ReadOrderDTO>(order);
            await EnrichOrderDtoAsync(dto);

            if (dto.Status != "Paid") dto.Schedule?.TourScheduleItineraries?.Clear();

            return dto;
        }

        public async Task<IEnumerable<ReadOrderDTO>> GetOrdersByScheduleIdAsync(int scheduleId)
        {
            var orders = await _orderRepository.GetByScheduleIdAsync(scheduleId);
            var orderDtos = orders.Select(async order =>
            {
                var dto = _mapper.Map<ReadOrderDTO>(order);
                await EnrichOrderDtoAsync(dto);
                return dto;
            });

            return await Task.WhenAll(orderDtos);
        }

        public async Task<PaginationDTO<ReadOrderDTO>> GetOrdersByUserIdAsync(int userId, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var (orders, total) = await _orderRepository.GetByUserIdPagedAsync(userId, page, pageSize);
            var orderDtos = await Task.WhenAll(orders.Select(async order =>
            {
                var dto = _mapper.Map<ReadOrderDTO>(order);
                await EnrichOrderDtoAsync(dto);
                return dto;
            }));

            return new PaginationDTO<ReadOrderDTO>
            {
                Data = orderDtos.ToList(),
                Total = total,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize)
            };
        }

     

        public async Task<bool> MarkOrderPaidAsync(int orderId, string customerEmail)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return false;

            if (order.Status == "Paid" || order.Status == "Completed")
            {
                return true;
            }

            var updated = await _orderRepository.UpdateStatusAsync(orderId, "Paid");
            if (!updated)
            {
                return false;
            }

            _backgroundJobService.EnqueueSendTicketsEmail(orderId, customerEmail);

            return true;
        }

        public async Task<bool> CancelOrderAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null) return false;

            if (order.Status == "Cancelled")
            {
                return true;
            }

            if (order.Status != "Pending")
            {
                return false;
            }

            var isCancelled = await _orderRepository.CancelOrderWithTicketsAsync(orderId);

            if (isCancelled)
            {
                await _tourApiClient.ReleaseScheduleSeatsAsync(order.ScheduleId, order.TicketCount);
            }

            return isCancelled;
        }

        private async Task ValidateScheduleForBookingAsync(
            int scheduleId,
            int ticketCount)
        {
            if (ticketCount <= 0)
            {
                throw new BookingValidationException("Ticket quantity must be at least 1.");
            }

            var schedule = await _tourApiClient.GetScheduleByIdAsync(scheduleId);
            if (schedule == null)
            {
                throw new BookingValidationException("Tour schedule not found.");
            }

            if (schedule.DepartureDate < DateTime.Now)
            {
                throw new BookingValidationException("This departure has expired and can no longer be booked.");
            }

            if (schedule.AvailableSeats < ticketCount)
            {
                throw new BookingValidationException(
                    schedule.AvailableSeats <= 0
                        ? "This departure is sold out."
                        : $"Only {schedule.AvailableSeats} seat(s) remaining for this departure.");
            }
        }

        private async Task EnrichOrderDtoAsync(ReadOrderDTO dto)
        {
            if (dto == null) return;

            dto.Schedule = await _tourApiClient.GetScheduleByIdAsync(dto.ScheduleId);

            if (dto.Schedule?.TourId > 0)
            {
                dto.Tour = await _tourApiClient.GetTourByIdAsync(dto.Schedule.TourId);

                if (dto.Tour?.Reviews != null)
                {
                    // 1. Tìm review của khách hàng này
                    var reviewDetail = dto.Tour.Reviews.FirstOrDefault(r => r.CustomerId == dto.CustomerId);

                    // 2. Nếu tìm thấy, tạo mới object OrderReviewDTO và gán dữ liệu vào
                    if (reviewDetail != null)
                    {
                        dto.Review = new OrderReviewDTO
                        {
                            Id = reviewDetail.Id,
                            Rating = reviewDetail.Rating,
                            Comment = reviewDetail.Comment
                        };
                    }
                }
            }
        }

    }
}
