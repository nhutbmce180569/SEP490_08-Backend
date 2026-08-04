using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Exceptions;
using BookingAPI.Models;
using BookingAPI.Repositories;
using BookingAPI.Services;
using BookingAPI.Services.Implements;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;

namespace BookingAPI.Services.Implements
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ITicketRepository _ticketRepository;
        private readonly IMapper _mapper;
        private readonly ITourApiClient _tourApiClient;
        private readonly IVoucherApiClient _voucherApiClient;
        private readonly IAuthApiClient _authApiClient;
        private readonly IBackgroundJobService _backgroundJobService;
        private readonly INotificationInternalService _notificationInternalService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IContentApiClient _contentApiClient;
        private readonly ILogger<OrderService> _logger;
        private readonly IIdempotencyService _idempotencyService;

        public OrderService(
         IOrderRepository orderRepository,
         ITicketRepository ticketRepository,
         IMapper mapper,
         ITourApiClient tourApiClient,
         IVoucherApiClient voucherApiClient,
         IAuthApiClient authApiClient,
         IBackgroundJobService backgroundJobService,
         INotificationInternalService notificationInternalService,
         IHttpClientFactory httpClientFactory,
         IHttpContextAccessor httpContextAccessor,
         IContentApiClient contentApiClient,
         ILogger<OrderService> logger,
         IIdempotencyService idempotencyService)
        {
            _orderRepository = orderRepository;
            _ticketRepository = ticketRepository;
            _mapper = mapper;
            _tourApiClient = tourApiClient;
            _voucherApiClient = voucherApiClient;
            _authApiClient = authApiClient;
            _backgroundJobService = backgroundJobService;
            _notificationInternalService = notificationInternalService;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _contentApiClient = contentApiClient;
            _logger = logger;
            _idempotencyService = idempotencyService;
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
        public async Task<ReadOrderDTO> CreateOrderAsync(int customerId, CreateOrderDTO request, string idempotencyKey)
        {
            if (request.OrderDetails == null || !request.OrderDetails.Any())
            {
                throw new BookingValidationException("At least one order detail is required.");
            }

            await ValidateScheduleForBookingAsync(request.ScheduleId);
            var detailRequests = await ValidateOrderDetailsAsync(request);

            var totalQuantity = detailRequests.Sum(x => x.Quantity);
            var totalAmount = detailRequests.Sum(x => x.TotalPrice);
            var totalPromotionDiscount = detailRequests.Sum(x => x.PromotionDiscountValue);

            var hasVoucherCode = !string.IsNullOrWhiteSpace(request.VoucherCode);
            if (!hasVoucherCode && request.DiscountValue.HasValue && request.DiscountValue.Value > 0)
            {
                throw new BookingValidationException("Discount without voucher is not allowed. Provide VoucherCode.");
            }

            var schedule = await _tourApiClient.GetScheduleByIdAsync(request.ScheduleId);
            var tourId = schedule?.TourId;

            long discountValue = 0;
            string? voucherCode = null;
            var voucherRedeemed = false;

            if (hasVoucherCode)
            {
                voucherCode = request.VoucherCode.Trim().ToUpperInvariant();
                try
                {
                    var redeemResult = await _voucherApiClient.RedeemVoucherAsync(new ApplyVoucherRequest
                    {
                        Code = voucherCode,
                        TourId = tourId,
                        BillAmount = totalAmount
                    });

                    discountValue = redeemResult.DiscountAmount;
                    voucherCode = redeemResult.Code;
                    voucherRedeemed = true;
                }
                catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
                {
                    throw new BookingValidationException(ex.Message);
                }
            }

            var order = new Order
            {
                CustomerId = customerId,
                ScheduleId = request.ScheduleId,
                TotalQuantity = totalQuantity,
                TotalAmount = totalAmount,
                DiscountValue = discountValue,
                PromotionDiscountValue = totalPromotionDiscount,
                VoucherCode = voucherCode,
                FinalAmount = Math.Max(0, totalAmount - discountValue - totalPromotionDiscount),
                Note = request.Note,
                Status = "Pending",
                InviteToken = Guid.NewGuid().ToString(),
                OrderedAt = DateTime.Now
            };

            foreach (var detailRequest in detailRequests)
            {
                var orderDetail = new OrderDetail
                {
                    TicketTypeId = detailRequest.TicketTypeId,
                    TourScheduleTicketId = detailRequest.TourScheduleTicketId,
                    Quantity = detailRequest.Quantity,
                    UnitPrice = detailRequest.UnitPrice,
                    TotalPrice = detailRequest.TotalPrice,
                    PromotionDiscountValue = detailRequest.PromotionDiscountValue,
                    Order = order
                };

                foreach (var ticketRequest in detailRequest.Tickets)
                {
                    var ticket = _mapper.Map<Ticket>(ticketRequest);
                    ticket.TicketTypeId = detailRequest.TicketTypeId;
                    ticket.CheckInStatus = "Pending";
                    ticket.QrCode = Guid.NewGuid().ToString();
                    ticket.OrderDetail = orderDetail;

                    orderDetail.Tickets.Add(ticket);
                }

                order.OrderDetails.Add(orderDetail);
            }

            var reservedDetails = new List<ValidatedOrderDetail>();
            var isOrderSaved = false;

            try
            {
                foreach (var detailRequest in detailRequests)
                {
                    var reserved = await _tourApiClient.ReserveScheduleTicketAsync(
                        detailRequest.TourScheduleTicketId,
                        detailRequest.Quantity);

                    if (!reserved)
                    {
                        throw new BookingValidationException("Could not reserve tickets for this departure. Please try again.");
                    }

                    reservedDetails.Add(detailRequest);
                }

                var savedOrder = await _orderRepository.AddAsync(order);
                isOrderSaved = true;

                await _idempotencyService.SetCompletedAsync(idempotencyKey, customerId, savedOrder.Id);

                _backgroundJobService.ScheduleAutoCancelOrder(savedOrder.Id);

                var dto = _mapper.Map<ReadOrderDTO>(savedOrder);
                await EnrichOrderDtoAsync(dto);
                return dto;
            }
            catch
            {
                if (!isOrderSaved)
                {
                    await _idempotencyService.DeleteProcessingKeyAsync(idempotencyKey, customerId);
                }

                await ReleaseReservedTicketsAsync(reservedDetails);

                if (voucherRedeemed && !string.IsNullOrWhiteSpace(voucherCode))
                {
                    try
                    {
                        await _voucherApiClient.RestoreVoucherAsync(customerId, voucherCode);
                    }
                    catch
                    {
                        // Best-effort rollback; order creation already failed.
                    }
                }

                throw;
            }
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

        public async Task<List<int>> GetCustomerIdsByScheduleIdAsync(int scheduleId)
        {
            if (scheduleId <= 0) return new List<int>();
            return await _orderRepository.GetCustomerIdsByScheduleIdAsync(scheduleId);
        }

        public async Task<List<int>> GetEligibleScheduleIdsByUserIdAsync(int userId)
        {
            if (userId <= 0) return new List<int>();
            return await _orderRepository.GetEligibleScheduleIdsByUserIdAsync(userId);
        }

        public async Task<IEnumerable<ScheduleCustomerDTO>> GetScheduleCustomersAsync(int scheduleId, string? attendeeName = null) // thêm param search
        {
            if (scheduleId <= 0)
                return Array.Empty<ScheduleCustomerDTO>();

            var tickets = await _ticketRepository.GetByScheduleIdAsync(scheduleId, attendeeName);
            if (tickets == null || !tickets.Any())
                return Array.Empty<ScheduleCustomerDTO>();

            var userIds = tickets
                .Where(t => t.UserId.HasValue)
                .Select(t => t.UserId!.Value)
                .Distinct()
                .ToList();

            var userProfiles = userIds.Any()
                ? await _authApiClient.GetUsersBatchAsync(userIds)
                : new List<BatchUserProfileDTO>();

            var userProfileMap = userProfiles
                .Where(u => u != null)
                .ToDictionary(u => u.Id, u => u);

            return tickets.Select(ticket =>
            {
                userProfileMap.TryGetValue(ticket.UserId ?? 0, out var profile);

                return new ScheduleCustomerDTO
                {
                    TicketId = ticket.Id,
                    OrderId = ticket.OrderDetail.OrderId,
                    UserId = ticket.UserId,
                    AttendeeName = ticket.AttendeeName,
                    IdCard = ticket.IdCard,
                    DateOfBirth = ticket.DateOfBirth,
                    Gender = ticket.Gender,
                    Nationality = ticket.Nationality,
                    PhoneNumber = profile?.PhoneNumber,
                };
            }).ToList();
        }

        public async Task<PaginationDTO<ReadOrderDTO>> GetOrdersByUserIdAsync(
            int userId,
            int page,
            int pageSize,
            string? status = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var (orders, total) = await _orderRepository.GetByUserIdPagedAsync(userId, page, pageSize, status);
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

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                _backgroundJobService.EnqueueSendTicketsEmail(orderId, customerEmail);
            }
            await NotifyBookingPaidAsync(order);

            // After marking order as paid, automatically add customer to the schedule chat room
            // This call should not crash the main flow if it fails
            try
            {
                await AddCustomerToChatRoomAsync(order.ScheduleId, order.CustomerId);
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - the order was already marked as paid successfully
                _logger.LogError(ex, $"Failed to add customer {order.CustomerId} to chat room for schedule {order.ScheduleId}. Error: {ex.Message}");
            }

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
                await ReleaseOrderTicketsAsync(order);
                await RestoreOrderVoucherAsync(order);
            }

            return isCancelled;
        }

        private async Task RestoreOrderVoucherAsync(Order order)
        {
            if (string.IsNullOrWhiteSpace(order.VoucherCode))
            {
                return;
            }

            if (order.Status != "Pending")
            {
                return;
            }

            try
            {
                await _voucherApiClient.RestoreVoucherAsync(order.CustomerId, order.VoucherCode);
            }
            catch
            {
                // Logged by caller context; cancellation should still succeed.
            }
        }

        private async Task ValidateScheduleForBookingAsync(int scheduleId)
        {
            var schedule = await _tourApiClient.GetScheduleByIdAsync(scheduleId);
            if (schedule == null)
            {
                throw new BookingValidationException("Tour schedule not found.");
            }

            if (schedule.DepartureDate < DateTime.Now)
            {
                throw new BookingValidationException("This departure has expired and can no longer be booked.");
            }

            var tour = await _tourApiClient.GetTourByIdAsync(schedule.TourId);
            if (tour == null || !string.Equals(tour.Status, "Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new BookingValidationException("This tour is not active and can no longer be booked.");
            }
        }

        private async Task<List<ValidatedOrderDetail>> ValidateOrderDetailsAsync(CreateOrderDTO request)
        {
            var duplicateTicket = request.OrderDetails
                .GroupBy(x => x.TourScheduleTicketId)
                .FirstOrDefault(x => x.Count() > 1);
            if (duplicateTicket != null)
            {
                throw new BookingValidationException(
                    $"TourScheduleTicketId {duplicateTicket.Key} is duplicated in this order.");
            }

            var totalTickets = request.OrderDetails.Sum(d => d.Tickets?.Count ?? 0);
            if (totalTickets > 9)
            {
                throw new BookingValidationException("You are exceeding StayHub's policy (maximum 9 tickets per order). Please contact our customer support for group bookings.");
            }

            var result = new List<ValidatedOrderDetail>();
            var ticketTypesCache = new Dictionary<int, TicketTypeResponseDTO?>();
            int totalChildTickets = 0;
            int totalAdultTickets = 0;

            foreach (var detail in request.OrderDetails)
            {
                if (detail.Tickets == null || !detail.Tickets.Any())
                {
                    throw new BookingValidationException("Each order detail must contain at least one ticket.");
                }

                var scheduleTicket = await _tourApiClient.GetScheduleTicketByIdAsync(detail.TourScheduleTicketId);
                if (scheduleTicket == null)
                {
                    throw new BookingValidationException(
                        $"Tour schedule ticket {detail.TourScheduleTicketId} not found.");
                }

                if (scheduleTicket.ScheduleId != request.ScheduleId)
                {
                    throw new BookingValidationException(
                        $"Tour schedule ticket {detail.TourScheduleTicketId} does not belong to schedule {request.ScheduleId}.");
                }

                if (!(scheduleTicket.IsActive ?? true))
                {
                    throw new BookingValidationException(
                        $"Tour schedule ticket {detail.TourScheduleTicketId} is inactive.");
                }

                var basePrice = scheduleTicket.Price;
                var priceInfo = GetEffectivePrice(basePrice, scheduleTicket.Promotion);
                var effectivePrice = priceInfo.EffectivePrice;
                var unitPromotionDiscount = priceInfo.PromotionDiscount;

                if (detail.UnitPrice.HasValue && detail.UnitPrice.Value != effectivePrice)
                {
                    throw new BookingValidationException(
                        "Ticket price has changed. Please return to the tour detail page to update the latest price.");
                }

                if (detail.TicketTypeId.HasValue && detail.TicketTypeId.Value != scheduleTicket.TicketTypeId)
                {
                    throw new BookingValidationException(
                        $"TicketTypeId does not match TourScheduleTicketId {detail.TourScheduleTicketId}.");
                }

                if (detail.Tickets.Any(ticket =>
                    ticket.TicketTypeId.HasValue &&
                    ticket.TicketTypeId.Value != scheduleTicket.TicketTypeId))
                {
                    throw new BookingValidationException(
                        $"Ticket contains a TicketTypeId that does not match TourScheduleTicketId {detail.TourScheduleTicketId}.");
                }

                var quantity = detail.Tickets.Count;
                if (scheduleTicket.AvailableQuantity < quantity)
                {
                    throw new BookingValidationException(
                        scheduleTicket.AvailableQuantity <= 0
                            ? "This ticket type is sold out."
                            : $"Only {scheduleTicket.AvailableQuantity} ticket(s) remaining for this ticket type.");
                }

                if (!ticketTypesCache.TryGetValue(scheduleTicket.TicketTypeId, out var ticketTypeInfo))
                {
                    ticketTypeInfo = await _contentApiClient.GetTicketTypeByIdAsync(scheduleTicket.TicketTypeId);
                    ticketTypesCache[scheduleTicket.TicketTypeId] = ticketTypeInfo;
                }

                if (ticketTypeInfo != null && (ticketTypeInfo.MinAge.HasValue || ticketTypeInfo.MaxAge.HasValue))
                {
                    foreach (var ticket in detail.Tickets)
                    {
                        if (!ticket.DateOfBirth.HasValue)
                        {
                            throw new BookingValidationException($"Date of birth is required for {ticketTypeInfo.Name} tickets.");
                        }

                        var age = CalculateAge(ticket.DateOfBirth.Value);
                        if (ticketTypeInfo.MinAge.HasValue && age < ticketTypeInfo.MinAge.Value)
                        {
                            throw new BookingValidationException($"Passenger {ticket.AttendeeName} ({age} years old) does not meet the minimum age requirement of {ticketTypeInfo.MinAge.Value} for {ticketTypeInfo.Name}.");
                        }
                        if (ticketTypeInfo.MaxAge.HasValue && age > ticketTypeInfo.MaxAge.Value)
                        {
                            throw new BookingValidationException($"Passenger {ticket.AttendeeName} ({age} years old) exceeds the maximum age limit of {ticketTypeInfo.MaxAge.Value} for {ticketTypeInfo.Name}.");
                        }
                    }
                }

                result.Add(new ValidatedOrderDetail
                {
                    TourScheduleTicketId = scheduleTicket.Id,
                    TicketTypeId = scheduleTicket.TicketTypeId,
                    Quantity = quantity,
                    UnitPrice = basePrice,
                    TotalPrice = basePrice * quantity,
                    PromotionDiscountValue = unitPromotionDiscount * quantity,
                    Tickets = detail.Tickets
                });

                var isChildTicket = false;
                if (ticketTypeInfo != null)
                {
                    var nameLower = ticketTypeInfo.Name.ToLower();
                    isChildTicket = nameLower.Contains("child") || nameLower.Contains("children") || (ticketTypeInfo.MaxAge.HasValue && ticketTypeInfo.MaxAge.Value <= 12);
                }

                if (isChildTicket)
                {
                    totalChildTickets += quantity;
                }
                else
                {
                    totalAdultTickets += quantity;
                }
            }

            if (totalChildTickets > totalAdultTickets)
            {
                throw new BookingValidationException("Each Child ticket must be accompanied by at least 1 Adult ticket.");
            }

            return result;
        }

        private static int CalculateAge(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth > today.AddYears(-age)) age--;
            return age;
        }

        private (long EffectivePrice, long PromotionDiscount) GetEffectivePrice(long basePrice, ReadPromotionDTO? promo)
        {
            if (promo == null || promo.Status != "Active")
                return (basePrice, 0);

            var now = DateTime.Now;
            if (promo.StartDate > now || promo.EndDate < now)
                return (basePrice, 0);

            if (promo.DiscountValue <= 0)
                return (basePrice, 0);

            decimal discountAmount = 0;
            if (promo.DiscountType.ToLower() == "percentage")
            {
                discountAmount = basePrice * (promo.DiscountValue / 100);
                if (promo.MaxDiscountAmount.HasValue && discountAmount > promo.MaxDiscountAmount.Value)
                {
                    discountAmount = promo.MaxDiscountAmount.Value;
                }
            }
            else
            {
                discountAmount = promo.DiscountValue;
            }

            long finalDiscount = (long)discountAmount;
            if (finalDiscount > basePrice) finalDiscount = basePrice;

            return (Math.Max(0, basePrice - finalDiscount), finalDiscount);
        }

        private async Task ReleaseOrderTicketsAsync(Order order)
        {
            foreach (var detail in order.OrderDetails)
            {
                await _tourApiClient.ReleaseScheduleTicketAsync(
                    detail.TourScheduleTicketId,
                    detail.Quantity);
            }
        }

        private async Task ReleaseReservedTicketsAsync(IEnumerable<ValidatedOrderDetail> details)
        {
            foreach (var detail in details)
            {
                await _tourApiClient.ReleaseScheduleTicketAsync(
                    detail.TourScheduleTicketId,
                    detail.Quantity);
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
                            Comment = reviewDetail.Comment,
                            CreatedAt = reviewDetail.CreatedAt
                        };
                    }
                }
            }
        }

        private async Task NotifyBookingPaidAsync(Order order)
        {
            try
            {
                var schedule = await _tourApiClient.GetScheduleByIdAsync(order.ScheduleId);
                var tour = schedule != null ? await _tourApiClient.GetTourByIdAsync(schedule.TourId) : null;

                var tourName = tour?.Name ?? "N/A";
                var departure = schedule?.DepartureDate.ToString("dd/MM/yyyy") ?? "N/A";
                var returnDate = schedule?.ReturnDate.ToString("dd/MM/yyyy") ?? "N/A";

                // Notify Customer
                await _notificationInternalService.NotifyUserAsync(
                        order.CustomerId,
                        "Booking payment successful",
                        $"Your booking for tour \"{tourName}\" " +
                        $"{departure} → {returnDate} " +
                        $"has been paid successfully. Your tickets are being sent to your email.");

                // Notify Manager
                if (tour != null && tour.OperatorId > 0)
                {
                    await _notificationInternalService.NotifyUserAsync(
                        tour.OperatorId,
                        "Customer Payment Successful",
                        $"Customer (ID: {order.CustomerId}) has successfully paid for the tour \"{tourName}\" " +
                        $"{departure} → {returnDate}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send paid booking notification for order {OrderId}.", order.Id);
            }
        }

        private async Task AddCustomerToChatRoomAsync(int scheduleId, int customerId)
        {
            try
            {
                var addMembersRequest = new
                {
                    userIds = new List<int> { customerId }
                };

                using var client = _httpClientFactory.CreateClient("SocialApiClient");
                client.DefaultRequestHeaders.Add("X-Internal-Key", "stayhub-internal-2025-xK9mP");

                var response = await client.PostAsJsonAsync(
                    $"api/chat/rooms/schedule/{scheduleId}/members",
                    addMembersRequest
                );

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Customer {customerId} automatically added to chat room for schedule {scheduleId} after checkout success.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Failed to add customer to chat room. Status: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error when automatically adding customer {customerId} to chat room: {ex.Message}");
            }
        }
    }
}