using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Models;
using BookingAPI.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BookingAPI.Services.Implements
{
    public class CancellationService : ICancellationService
    {
        private readonly ICancellationRepository _repository;
        private readonly IMapper _mapper;
        private readonly INotificationInternalService _notificationService;
        private readonly IAuthApiClient _authApiClient;
        private readonly ITourApiClient _tourApiClient;

        public CancellationService(
            ICancellationRepository repository,
            IMapper mapper,
            INotificationInternalService notificationService,
            IAuthApiClient authApiClient,
            ITourApiClient tourApiClient)
        {
            _repository = repository;
            _mapper = mapper;
            _notificationService = notificationService;
            _authApiClient = authApiClient;
            _tourApiClient = tourApiClient;
        }

        public async Task<CancellationRequestDetailDTO> CreateCancellationRequestAsync(int customerId, CreateCancellationRequestDTO dto)
        {
            var order = await _repository.GetOrderForCancellationAsync(dto.OrderId, customerId);

            if (order == null)
                throw new Exception("Order not found or does not belong to you.");

            if (order.Status != "Paid" && order.Status != "Completed")
                throw new Exception($"Cannot request cancellation because the order is in status: {order.Status}");

            if (order.CancellationRequests.Any(r => r.Status == "Pending"))
                throw new InvalidOperationException("PendingCancellationExists");

            DateTime scheduleStartDate = DateTime.UtcNow.AddDays(10); 

            int daysUntilTour = (scheduleStartDate.Date - DateTime.UtcNow.Date).Days;

            if (daysUntilTour <= 1)
                throw new Exception("Cancellation is not allowed 1 day before or on the departure date.");

            int feePercent = 0;

            if (daysUntilTour >= 15)
            {
                feePercent = 5;
            }
            else if (daysUntilTour >= 10)
            {
                feePercent = 10;
            }
            else if (daysUntilTour >= 5)
            {
                feePercent = 15;
            }
            else if (daysUntilTour >= 2)
            {
                feePercent = 20;
            }

            long originalAmount = order.FinalAmount;
            long cancellationFee = (originalAmount * feePercent) / 100;
            long refundAmount = originalAmount - cancellationFee;

            var cancellationRequest = new CancellationRequest
            {
                OrderId = dto.OrderId,
                CustomerId = customerId,
                BankName = dto.BankName,
                AccountNumber = dto.AccountNumber,
                AccountHolderName = dto.AccountHolderName,
                Reason = dto.Reason,
                OriginalAmount = originalAmount,
                FeePercent = feePercent,
                CancellationFee = cancellationFee,
                RefundAmount = refundAmount,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            order.Status = "Request to Cancel";

            await _repository.CreateCancellationRequestAsync(cancellationRequest);

            try
            {
                var schedule = await _tourApiClient.GetScheduleByIdAsync(order.ScheduleId);
                if (schedule != null)
                {
                    var tour = await _tourApiClient.GetTourByIdAsync(schedule.TourId);
                    if (tour != null && tour.OperatorId > 0)
                    {
                        string title = "New Cancellation Request";
                        string content = $"There is a new cancellation request (Order #{order.Id}) from customer (ID: {customerId}) for tour '{tour.Name}'.";
                        await _notificationService.NotifyUserAsync(tour.OperatorId, title, content);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to send notification to manager: {ex.Message}");
            }

            return _mapper.Map<CancellationRequestDetailDTO>(cancellationRequest);
        }

        public async Task<PaginationDTO<CancellationRequestListDTO>> GetCancellationRequestsAsync(
            int operatorId,
            string? status,
            string? date,
            int? tourId,
            int page,
            int pageSize)
        {
            var scheduleIds = await _tourApiClient.GetManagedScheduleIdsAsync(operatorId, tourId);
            var (requests, total) = await _repository.GetAllCancellationRequestsAsync(
                scheduleIds,
                status,
                date,
                page,
                pageSize);

            var mappedData = _mapper.Map<List<CancellationRequestListDTO>>(requests);

            var scheduleCache = new Dictionary<int, ReadOrderScheduleDTO?>();
            var tourCache = new Dictionary<int, ReadOrderTourDTO?>();

            foreach (var item in mappedData)
            {
                var req = requests.FirstOrDefault(r => r.Id == item.Id);
                if (req?.Order != null && req.Order.ScheduleId > 0)
                {
                    if (!scheduleCache.TryGetValue(req.Order.ScheduleId, out var schedule))
                    {
                        schedule = await _tourApiClient.GetScheduleByIdAsync(req.Order.ScheduleId);
                        scheduleCache[req.Order.ScheduleId] = schedule;
                    }

                    if (schedule != null && schedule.TourId > 0)
                    {
                        item.TourId = schedule.TourId;
                        if (!tourCache.TryGetValue(schedule.TourId, out var tour))
                        {
                            tour = await _tourApiClient.GetTourByIdAsync(schedule.TourId);
                            tourCache[schedule.TourId] = tour;
                        }
                        item.TourName = tour?.Name;
                    }
                }
            }

            var totalPages = (int)Math.Ceiling(total / (double)pageSize);

            return new PaginationDTO<CancellationRequestListDTO>
            {
                Data = mappedData,
                Total = total,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<CancellationRequestDetailDTO> GetCancellationRequestDetailsAsync(int id, int operatorId)
        {
            var request = await _repository.GetCancellationRequestByIdAsync(id);
            if (request == null)
                throw new Exception("Cancellation request not found.");

            await EnsureOperatorCanManageAsync(request, operatorId);

            var dto = _mapper.Map<CancellationRequestDetailDTO>(request);

            var users = await _authApiClient.GetUserProfileAsync(request.CustomerId);

            Console.WriteLine("\n========== TEST USER BATCH API ==========");
            Console.WriteLine($"Requested CustomerId: {request.CustomerId}");
            Console.WriteLine($"Response Users: {System.Text.Json.JsonSerializer.Serialize(users, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}");
            Console.WriteLine("=========================================\n");

            dto.Customer = users;

            if (request.Order != null && request.Order.ScheduleId > 0)
            {
                var schedule = await _tourApiClient.GetScheduleByIdAsync(request.Order.ScheduleId);
                if (schedule != null && schedule.TourId > 0)
                {
                    dto.Tour = await _tourApiClient.GetTourByIdAsync(schedule.TourId);
                }
            }

            return dto;
        }

        public async Task<CancellationRequestDetailDTO> ProcessCancellationRequestAsync(
            int id,
            int operatorId,
            ProcessCancellationDTO dto)
        {
            var request = await _repository.GetCancellationRequestByIdAsync(id);

            if (request == null)
                throw new Exception("Cancellation request not found.");

            await EnsureOperatorCanManageAsync(request, operatorId);

            if (request.Status != "Pending")
                throw new Exception($"Cannot process this request. It is already marked as '{request.Status}'.");

            request.ProcessedBy = operatorId;
            request.ProcessedAt = DateTime.UtcNow;

            if (dto.Action == "Approve")
            {
                request.Status = "Approved";
                if (request.Order != null)
                {
                    request.Order.Status = "Cancelled";
                }
            }
            else if (dto.Action == "Reject")
            {
                if (string.IsNullOrWhiteSpace(dto.RejectReason))
                    throw new Exception("Reject reason is required when rejecting a request.");

                request.Status = "Rejected";
                request.RejectReason = dto.RejectReason;
                if (request.Order != null)
                {
                    request.Order.Status = "Paid";
                }
            }
            else
            {
                throw new Exception("Invalid action. Action must be 'Approve' or 'Reject'.");
            }

            await _repository.UpdateCancellationRequestAsync(request);

            try
            {
                if (dto.Action == "Approve")
                {
                    string title = "Cancellation Request Approved";
                    string content = $"Your cancellation request for order #{request.OrderId} has been successfully approved. You will receive a refund of {request.RefundAmount:N0} VND shortly.";
                    await _notificationService.NotifyUserAsync(request.CustomerId, title, content);
                }
                else if (dto.Action == "Reject")
                {
                    string title = "Cancellation Request Rejected";
                    string content = $"Your cancellation request for order #{request.OrderId} has been rejected. Reason: {request.RejectReason}";
                    await _notificationService.NotifyUserAsync(request.CustomerId, title, content);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Warning] Failed to send notification: {ex.Message}");
            }

            return _mapper.Map<CancellationRequestDetailDTO>(request);
        }

        private async Task EnsureOperatorCanManageAsync(CancellationRequest request, int operatorId)
        {
            if (request.Order == null)
                throw new Exception("Order information for this cancellation request was not found.");

            var scheduleIds = await _tourApiClient.GetManagedScheduleIdsAsync(operatorId);
            if (!scheduleIds.Contains(request.Order.ScheduleId))
                throw new UnauthorizedAccessException("You do not have permission to access this cancellation request.");
        }
    }
}
