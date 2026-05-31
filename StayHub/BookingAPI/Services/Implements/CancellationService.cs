using AutoMapper;
using BookingAPI.DTOs;
using BookingAPI.Models;
using BookingAPI.Repositories;

namespace BookingAPI.Services.Implements
{
    public class CancellationService : ICancellationService
    {
        private readonly ICancellationRepository _repository;
        private readonly IMapper _mapper;

        public CancellationService(ICancellationRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<CancellationRequestDetailDTO> CreateCancellationRequestAsync(int customerId, CreateCancellationRequestDTO dto)
        {
            var order = await _repository.GetOrderForCancellationAsync(dto.OrderId, customerId);

            if (order == null)
                throw new Exception("Order not found or does not belong to you.");

            if (order.Status != "Paid" && order.Status != "Completed")
                throw new Exception($"Cannot request cancellation because the order is in status: {order.Status}");

            if (order.CancellationRequests.Any(r => r.Status == "Pending"))
                throw new Exception("There is already a pending cancellation request for this order.");

            int feePercent = 10;
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

            await _repository.CreateCancellationRequestAsync(cancellationRequest);

            // Dùng AutoMapper
            return _mapper.Map<CancellationRequestDetailDTO>(cancellationRequest);
        }

        public async Task<IEnumerable<CancellationRequestListDTO>> GetCancellationRequestsAsync(string? status)
        {
            var requests = await _repository.GetAllCancellationRequestsAsync(status);

            return _mapper.Map<IEnumerable<CancellationRequestListDTO>>(requests);
        }

        public async Task<CancellationRequestDetailDTO> GetCancellationRequestDetailsAsync(int id)
        {
            var request = await _repository.GetCancellationRequestByIdAsync(id);
            if (request == null)
                throw new Exception("Cancellation request not found.");

            // Dùng AutoMapper
            return _mapper.Map<CancellationRequestDetailDTO>(request);
        }

        public async Task<CancellationRequestDetailDTO> ProcessCancellationRequestAsync(int id, int processedByUserId, ProcessCancellationDTO dto)
        {
            var request = await _repository.GetCancellationRequestByIdAsync(id);

            if (request == null)
                throw new Exception("Cancellation request not found.");

            if (request.Status != "Pending")
                throw new Exception($"Cannot process this request. It is already marked as '{request.Status}'.");

            request.ProcessedBy = processedByUserId;
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
            }
            else
            {
                throw new Exception("Invalid action. Action must be 'Approve' or 'Reject'.");
            }

            await _repository.UpdateCancellationRequestAsync(request);

            return _mapper.Map<CancellationRequestDetailDTO>(request);
        }
    }
}