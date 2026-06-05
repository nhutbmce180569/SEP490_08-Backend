using AutoMapper;
using VoucherAPI.DTOs;
using VoucherAPI.Models;
using VoucherAPI.Repositories;

namespace VoucherAPI.Services.Implements;

public class VoucherService : IVoucherService
{
    private readonly IVoucherRepository _voucherRepository;
    private readonly IUserVoucherRepository _userVoucherRepository;
    private readonly ITourValidationService _tourValidationService;
    private readonly IUserValidationService _userValidationService;
    private readonly IBookingAnalyticsClient _bookingAnalyticsClient;
    private readonly IMapper _mapper;

    public VoucherService(
        IVoucherRepository voucherRepository,
        IUserVoucherRepository userVoucherRepository,
        ITourValidationService tourValidationService,
        IUserValidationService userValidationService,
        IBookingAnalyticsClient bookingAnalyticsClient,
        IMapper mapper)
    {
        _voucherRepository = voucherRepository;
        _userVoucherRepository = userVoucherRepository;
        _tourValidationService = tourValidationService;
        _userValidationService = userValidationService;
        _bookingAnalyticsClient = bookingAnalyticsClient;
        _mapper = mapper;
    }

    public async Task<PaginationDTO<ReadVoucherDTO>> GetAll(
        int page,
        int pageSize,
        string? search,
        int? tourId,
        string? discountType,
        string? status,
        bool? isActive)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var entities = await _voucherRepository.GetAllAsync();
        var list = new List<ReadVoucherDTO>();

        foreach (var entity in entities)
        {
            var dto = _mapper.Map<ReadVoucherDTO>(entity);
            await EnrichVoucherAsync(dto, entity);
            list.Add(dto);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToUpperInvariant();
            list = list.Where(v =>
                v.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                (v.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }

        if (tourId.HasValue)
        {
            list = list.Where(v => v.TourId == tourId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(discountType))
        {
            list = list.Where(v =>
                v.DiscountType.Equals(discountType.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (isActive.HasValue)
        {
            list = list.Where(v => v.IsActive == isActive.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            list = list.Where(v =>
                v.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var total = list.Count;
        var paged = list
            .OrderByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginationDTO<ReadVoucherDTO>
        {
            Data = paged,
            Total = total,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize),
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<ReadVoucherDetailDTO?> GetById(int id)
    {
        var entity = await _voucherRepository.GetByIdWithUserVouchersAsync(id);
        if (entity == null) return null;

        var dto = _mapper.Map<ReadVoucherDetailDTO>(entity);
        await EnrichVoucherAsync(dto, entity);

        foreach (var assignment in dto.AssignedCustomers)
        {
            var userInfo = await _userValidationService.ValidateUserAsync(assignment.UserId);
            assignment.UserFullName = userInfo.FullName;
            assignment.UserEmail = userInfo.Email;
        }

        return dto;
    }

    public async Task<ReadVoucherDetailDTO> Create(CreateVoucherDTO dto, int creatorId)
    {
        dto.Code = dto.Code.Trim().ToUpperInvariant();
        ValidateDateRange(dto.StartDate, dto.EndDate);
        ValidateDiscount(dto.DiscountType, dto.DiscountValue);
        ValidateMaxDiscountAmount(dto.DiscountType, dto.MaxDiscountAmount);

        if (await _voucherRepository.CodeExistsAsync(dto.Code))
        {
            throw new Exception($"Voucher code '{dto.Code}' already exists");
        }

        if (dto.TourId.HasValue)
        {
            var tour = await _tourValidationService.ValidateTourAsync(dto.TourId.Value);
            if (!tour.Exists)
            {
                throw new Exception($"Tour with Id {dto.TourId.Value} not found");
            }

            if (tour.Status != null && !tour.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Tour with Id {dto.TourId.Value} is not active");
            }
        }

        var assignments = await ResolveCustomerAssignmentsAsync(dto.CustomerAssignments, dto.TopCustomerAssignment);
        if (assignments.Count > 0)
        {
            ValidateCustomerAssignments(assignments);
            await ValidateCustomersAsync(assignments);

            var totalAssignedQuantity = assignments.Sum(a => a.Quantity);
            if (totalAssignedQuantity > dto.AvailableCount)
            {
                throw new Exception("Total assigned quantity cannot exceed AvailableCount");
            }
        }

        var entity = _mapper.Map<Voucher>(dto);
        entity.CreatorId = creatorId;
        ApplyMaxDiscountAmount(entity);

        var userVouchers = assignments.Select(a => new UserVoucher
        {
            UserId = a.UserId,
            Quantity = a.Quantity,
            Status = "Available"
        }).ToList();

        if (userVouchers.Count > 0)
        {
            await _voucherRepository.AddWithAssignmentsAsync(entity, userVouchers);
        }
        else
        {
            await _voucherRepository.AddAsync(entity);
        }

        return (await GetById(entity.Id))!;
    }

    public async Task<ReadVoucherDetailDTO> Update(int id, UpdateVoucherDTO dto)
    {
        var entity = await _voucherRepository.GetByIdWithUserVouchersAsync(id);
        if (entity == null)
        {
            throw new Exception("Voucher not found");
        }

        if (!entity.IsActive)
        {
            throw new Exception("Cannot update a deactivated voucher");
        }

        var newStartDate = dto.StartDate ?? entity.StartDate;
        var newEndDate = dto.EndDate ?? entity.EndDate;
        ValidateDateRange(newStartDate, newEndDate);

        if (entity.UsedCount > 0)
        {
            if (dto.DiscountType != null && !dto.DiscountType.Equals(entity.DiscountType, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Cannot change DiscountType after voucher has been used");
            }

            if (dto.DiscountValue.HasValue && dto.DiscountValue.Value != entity.DiscountValue)
            {
                throw new Exception("Cannot change DiscountValue after voucher has been used");
            }

            if (dto.MaxDiscountAmount.HasValue && dto.MaxDiscountAmount != entity.MaxDiscountAmount)
            {
                throw new Exception("Cannot change MaxDiscountAmount after voucher has been used");
            }

            if (dto.TourId.HasValue && dto.TourId != entity.TourId)
            {
                throw new Exception("Cannot change TourId after voucher has been used");
            }
        }

        if (dto.TourId.HasValue)
        {
            var tour = await _tourValidationService.ValidateTourAsync(dto.TourId.Value);
            if (!tour.Exists)
            {
                throw new Exception($"Tour with Id {dto.TourId.Value} not found");
            }

            if (tour.Status != null && !tour.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Tour with Id {dto.TourId.Value} is not active");
            }

            entity.TourId = dto.TourId;
        }

        if (dto.DiscountType != null)
        {
            var discountValue = dto.DiscountValue ?? entity.DiscountValue;
            ValidateDiscount(dto.DiscountType, discountValue);
            entity.DiscountType = dto.DiscountType;
        }

        if (dto.DiscountValue.HasValue)
        {
            ValidateDiscount(entity.DiscountType, dto.DiscountValue.Value);
            entity.DiscountValue = dto.DiscountValue.Value;
        }

        if (dto.MaxDiscountAmount.HasValue)
        {
            ValidateMaxDiscountAmount(entity.DiscountType, dto.MaxDiscountAmount);
            entity.MaxDiscountAmount = dto.MaxDiscountAmount;
        }
        else if (dto.DiscountType != null &&
                 dto.DiscountType.Equals("Amount", StringComparison.OrdinalIgnoreCase))
        {
            entity.MaxDiscountAmount = null;
        }

        ApplyMaxDiscountAmount(entity);
        ValidateMaxDiscountAmount(entity.DiscountType, entity.MaxDiscountAmount);

        if (dto.AvailableCount.HasValue)
        {
            if (dto.AvailableCount.Value < entity.UsedCount)
            {
                throw new Exception("AvailableCount cannot be less than UsedCount");
            }

            entity.AvailableCount = dto.AvailableCount.Value;
        }

        if (dto.StartDate.HasValue)
        {
            entity.StartDate = dto.StartDate.Value;
        }

        if (dto.EndDate.HasValue)
        {
            entity.EndDate = dto.EndDate.Value;
        }

        if (dto.Description != null)
        {
            entity.Description = dto.Description;
        }

        await _voucherRepository.UpdateAsync(entity);

        var resolvedAssignments = await ResolveCustomerAssignmentsAsync(
            dto.CustomerAssignments,
            dto.TopCustomerAssignment);

        if (resolvedAssignments.Count > 0)
        {
            ValidateCustomerAssignments(resolvedAssignments);
            await ValidateCustomersAsync(resolvedAssignments);

            var newAssignments = new List<UserVoucher>();
            foreach (var assignment in resolvedAssignments)
            {
                if (await _userVoucherRepository.ExistsForUserAsync(entity.Id, assignment.UserId))
                {
                    throw new Exception($"Customer with Id {assignment.UserId} is already assigned to this voucher");
                }

                newAssignments.Add(new UserVoucher
                {
                    UserId = assignment.UserId,
                    VoucherId = entity.Id,
                    Quantity = assignment.Quantity,
                    Status = "Available"
                });
            }

            var totalAssigned = entity.UserVouchers.Sum(uv => uv.Quantity)
                + newAssignments.Sum(uv => uv.Quantity);
            if (totalAssigned > entity.AvailableCount)
            {
                throw new Exception("Total assigned quantity cannot exceed AvailableCount");
            }

            await _userVoucherRepository.AddRangeAsync(newAssignments);
        }

        return (await GetById(entity.Id))!;
    }

    public async Task<ReadVoucherDTO> Activate(int id)
    {
        var entity = await _voucherRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception("Voucher not found");
        }

        if (entity.IsActive)
        {
            throw new Exception("Voucher is already active");
        }

        if (entity.EndDate < DateTime.Now)
        {
            throw new Exception("Cannot activate an expired voucher");
        }

        if (entity.UsedCount >= entity.AvailableCount)
        {
            throw new Exception("Cannot activate a voucher that has reached its usage limit");
        }

        if (entity.TourId.HasValue)
        {
            var tour = await _tourValidationService.ValidateTourAsync(entity.TourId.Value);
            if (!tour.Exists)
            {
                throw new Exception($"Tour with Id {entity.TourId.Value} not found");
            }

            if (tour.Status != null && !tour.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Tour with Id {entity.TourId.Value} is not active");
            }
        }

        await _voucherRepository.SetActiveAsync(entity, true);

        var dto = _mapper.Map<ReadVoucherDTO>(entity);
        await EnrichVoucherAsync(dto, entity);
        return dto;
    }

    public async Task<ReadVoucherDTO> Deactivate(int id)
    {
        var entity = await _voucherRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception("Voucher not found");
        }

        if (!entity.IsActive)
        {
            throw new Exception("Voucher is already deactivated");
        }

        await _voucherRepository.SetActiveAsync(entity, false);

        var dto = _mapper.Map<ReadVoucherDTO>(entity);
        await EnrichVoucherAsync(dto, entity);
        return dto;
    }

    private async Task EnrichVoucherAsync(ReadVoucherDTO dto, Voucher entity)
    {
        if (entity.TourId.HasValue)
        {
            var tour = await _tourValidationService.ValidateTourAsync(entity.TourId.Value);
            dto.TourName = tour.Name;
        }

        var creator = await _userValidationService.ValidateUserAsync(entity.CreatorId);
        dto.CreatorName = creator.FullName;
    }

    private async Task ValidateCustomersAsync(IEnumerable<CreateUserVoucherAssignmentDTO> assignments)
    {
        var duplicateUserIds = assignments
            .GroupBy(a => a.UserId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateUserIds.Count > 0)
        {
            throw new Exception($"Duplicate customer assignments found for UserIds: {string.Join(", ", duplicateUserIds)}");
        }

        foreach (var assignment in assignments)
        {
            var user = await _userValidationService.ValidateUserAsync(assignment.UserId);
            if (!user.Exists)
            {
                throw new Exception($"Customer with Id {assignment.UserId} not found");
            }

            if (user.Status != null && !user.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Customer with Id {assignment.UserId} is not active");
            }
        }
    }

    private async Task<List<CreateUserVoucherAssignmentDTO>> ResolveCustomerAssignmentsAsync(
        List<CreateUserVoucherAssignmentDTO>? customerAssignments,
        TopCustomerVoucherAssignmentDTO? topCustomerAssignment)
    {
        var assignments = customerAssignments ?? [];
        var hasSpecific = assignments.Count > 0;
        var hasTop = topCustomerAssignment != null;

        if (hasSpecific && hasTop)
        {
            throw new Exception("Cannot use both specific customer assignments and top customer assignment");
        }

        if (!hasTop)
        {
            return assignments;
        }

        ValidateTopCustomerAssignment(topCustomerAssignment!);
        var (from, to) = ResolveRevenuePeriod(topCustomerAssignment!.RevenuePeriod);
        var topCustomers = await _bookingAnalyticsClient.GetTopCustomersAsync(
            topCustomerAssignment.Top,
            from,
            to);

        if (topCustomers.Count == 0)
        {
            throw new Exception("No customers found for the selected revenue period");
        }

        return topCustomers
            .Select(customer => new CreateUserVoucherAssignmentDTO
            {
                UserId = customer.CustomerId,
                Quantity = topCustomerAssignment.Quantity
            })
            .ToList();
    }

    private static void ValidateTopCustomerAssignment(TopCustomerVoucherAssignmentDTO assignment)
    {
        if (assignment.Top <= 0 || assignment.Top > 100)
        {
            throw new Exception("Top must be between 1 and 100");
        }

        if (assignment.Quantity <= 0)
        {
            throw new Exception("Quantity must be at least 1");
        }

        if (!assignment.RevenuePeriod.Equals("Month", StringComparison.OrdinalIgnoreCase)
            && !assignment.RevenuePeriod.Equals("Year", StringComparison.OrdinalIgnoreCase)
            && !assignment.RevenuePeriod.Equals("AllTime", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("RevenuePeriod must be 'Month', 'Year', or 'AllTime'");
        }
    }

    private static (DateTime? From, DateTime? To) ResolveRevenuePeriod(string revenuePeriod)
    {
        var now = DateTime.UtcNow;

        if (revenuePeriod.Equals("Month", StringComparison.OrdinalIgnoreCase))
        {
            return (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), now);
        }

        if (revenuePeriod.Equals("Year", StringComparison.OrdinalIgnoreCase))
        {
            return (new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), now);
        }

        if (revenuePeriod.Equals("AllTime", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        throw new Exception("RevenuePeriod must be 'Month', 'Year', or 'AllTime'");
    }

    private static void ValidateCustomerAssignments(IEnumerable<CreateUserVoucherAssignmentDTO> assignments)
    {
        foreach (var assignment in assignments)
        {
            if (assignment.UserId <= 0)
            {
                throw new Exception("UserId must be greater than 0");
            }

            if (assignment.Quantity <= 0)
            {
                throw new Exception("Quantity must be at least 1");
            }
        }
    }

    private static void ValidateDateRange(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
        {
            throw new Exception("EndDate must be later than StartDate");
        }
    }

    private static void ValidateDiscount(string discountType, long discountValue)
    {
        if (discountType.Equals("Percent", StringComparison.OrdinalIgnoreCase))
        {
            if (discountValue < 1 || discountValue > 100)
            {
                throw new Exception("Percent discount must be between 1 and 100");
            }
        }
        else if (discountType.Equals("Amount", StringComparison.OrdinalIgnoreCase))
        {
            if (discountValue <= 0)
            {
                throw new Exception("Amount discount must be greater than 0");
            }
        }
        else
        {
            throw new Exception("DiscountType must be 'Percent' or 'Amount'");
        }
    }

    private static void ValidateMaxDiscountAmount(string discountType, long? maxDiscountAmount)
    {
        if (discountType.Equals("Percent", StringComparison.OrdinalIgnoreCase))
        {
            if (!maxDiscountAmount.HasValue || maxDiscountAmount.Value <= 0)
            {
                throw new Exception("MaxDiscountAmount is required for Percent vouchers and must be greater than 0");
            }
        }
        else if (discountType.Equals("Amount", StringComparison.OrdinalIgnoreCase))
        {
            if (maxDiscountAmount.HasValue)
            {
                throw new Exception("MaxDiscountAmount only applies to Percent vouchers");
            }
        }
    }

    private static void ApplyMaxDiscountAmount(Voucher entity)
    {
        if (entity.DiscountType.Equals("Amount", StringComparison.OrdinalIgnoreCase))
        {
            entity.MaxDiscountAmount = null;
        }
    }
}
