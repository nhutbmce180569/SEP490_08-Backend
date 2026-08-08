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
    private readonly IEmailService _emailService;
    private readonly INotificationInternalService _notificationInternalService;
    private readonly IMapper _mapper;

    public VoucherService(
        IVoucherRepository voucherRepository,
        IUserVoucherRepository userVoucherRepository,
        ITourValidationService tourValidationService,
        IUserValidationService userValidationService,
        IBookingAnalyticsClient bookingAnalyticsClient,
        IEmailService emailService,
        INotificationInternalService notificationInternalService,
        IMapper mapper)
    {
        _voucherRepository = voucherRepository;
        _userVoucherRepository = userVoucherRepository;
        _tourValidationService = tourValidationService;
        _userValidationService = userValidationService;
        _bookingAnalyticsClient = bookingAnalyticsClient;
        _emailService = emailService;
        _notificationInternalService = notificationInternalService;
        _mapper = mapper;
    }

    public async Task<PaginationDTO<ReadVoucherDTO>> GetAll(
        int page,
        int pageSize,
        string? search,
        int? tourId,
        string? discountType,
        string? status,
        bool? isActive,
        bool? createdByMe,
        int currentUserId,
        string? voucherType,
        bool isAdmin = false)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var entities = await _voucherRepository.GetAllAsync();
        var list = entities.ToList();

        if (isAdmin)
        {
            // Admin manages system-wide vouchers (TourId == null) or vouchers created by Admin
            list = list.Where(v => v.TourId == null || v.CreatorId == currentUserId).ToList();
        }
        else
        {
            // Manager only manages vouchers created by that manager
            list = list.Where(v => v.CreatorId == currentUserId).ToList();
        }

        if (createdByMe == true)
        {
            list = list.Where(v => v.CreatorId == currentUserId).ToList();
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

        if (!string.IsNullOrWhiteSpace(voucherType))
        {
            if (voucherType.Equals("birthday", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(v => v.Code.StartsWith("BDAY_", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (voucherType.Equals("tour", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(v => !v.Code.StartsWith("BDAY_", StringComparison.OrdinalIgnoreCase)).ToList();
            }
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
            {
                // To filter by status, we temporarily resolve it
                var s = ResolveStatus(v);
                return s.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase);
            }).ToList();
        }

        var total = list.Count;
        var pagedEntities = list
            .OrderByDescending(v => v.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var pagedDtos = new List<ReadVoucherDTO>();
        foreach (var entity in pagedEntities)
        {
            var dto = _mapper.Map<ReadVoucherDTO>(entity);
            await EnrichVoucherAsync(dto, entity);
            pagedDtos.Add(dto);
        }

        return new PaginationDTO<ReadVoucherDTO>
        {
            Data = pagedDtos,
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

        if (dto.AssignedCustomers.Count > 0)
        {
            var userIds = dto.AssignedCustomers.Select(a => a.UserId).Distinct().ToList();
            var userList = await _userValidationService.GetUsersBatchAsync(userIds);
            var userMap = userList.ToDictionary(u => u.Id, u => u);

            foreach (var assignment in dto.AssignedCustomers)
            {
                if (userMap.TryGetValue(assignment.UserId, out var userInfo))
                {
                    assignment.UserFullName = userInfo.FullName;
                    assignment.UserEmail = userInfo.Email;
                }
                else
                {
                    // Fallback to single user lookup if missing in batch
                    var singleUser = await _userValidationService.ValidateUserAsync(assignment.UserId);
                    if (singleUser.Exists)
                    {
                        assignment.UserFullName = singleUser.FullName;
                        assignment.UserEmail = singleUser.Email;
                    }
                }
            }
        }

        return dto;
    }

    public async Task<ReadVoucherDetailDTO> Create(CreateVoucherDTO dto, int creatorId, bool isAdmin)
    {
        dto.Code = dto.Code.Trim().ToUpperInvariant();
        ValidateDateRange(dto.StartDate, dto.EndDate);
        ValidateDiscount(dto.DiscountType, dto.DiscountValue);
        ValidateMaxDiscountAmount(dto.DiscountType, dto.MaxDiscountAmount);

        if (!isAdmin && !dto.TourId.HasValue)
        {
            throw new Exception("Managers can only create vouchers for their own tours, but TourId was not provided");
        }

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

            if (!isAdmin && tour.CreatedBy != creatorId)
            {
                throw new Exception("You do not have permission to create a voucher for a tour you did not create");
            }
        }

        var assignments = await ResolveCustomerAssignmentsAsync(dto.CustomerAssignments, dto.TopCustomerAssignment);
        
        if (assignments.Count == 0)
        {
            var activeUserIds = await _userValidationService.GetAllActiveCustomerIdsAsync();
            var limit = dto.AvailableCount;
            assignments = activeUserIds.Take(limit).Select(id => new CreateUserVoucherAssignmentDTO
            {
                UserId = id,
                Quantity = 1
            }).ToList();
        }

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

    public async Task<ReadVoucherDetailDTO> Update(int id, UpdateVoucherDTO dto, int currentUserId, bool isAdmin)
    {
        var entity = await _voucherRepository.GetByIdWithUserVouchersAsync(id);
        if (entity == null)
        {
            throw new Exception("Voucher not found");
        }

        if (!isAdmin && entity.CreatorId != currentUserId)
        {
            throw new Exception("You can only edit vouchers that you created");
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

            if (!isAdmin && tour.CreatedBy != currentUserId)
            {
                throw new Exception("You do not have permission to assign this voucher to a tour you did not create");
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

        if (dto.MinOrderAmount.HasValue)
        {
            entity.MinOrderAmount = dto.MinOrderAmount.Value <= 0 ? null : dto.MinOrderAmount.Value;
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

    public async Task<ReadVoucherDTO> Activate(int id, int currentUserId, bool isAdmin)
    {
        var entity = await _voucherRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception("Voucher not found");
        }

        if (!isAdmin && entity.CreatorId != currentUserId)
        {
            throw new Exception("You can only activate vouchers that you created");
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

    public async Task<ReadVoucherDTO> Deactivate(int id, int currentUserId, bool isAdmin)
    {
        var entity = await _voucherRepository.GetByIdAsync(id);
        if (entity == null)
        {
            throw new Exception("Voucher not found");
        }

        if (!isAdmin && entity.CreatorId != currentUserId)
        {
            throw new Exception("You can only deactivate vouchers that you created");
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
        var (from, to) = ResolveRevenuePeriod(topCustomerAssignment!);
        var topCustomers = await _bookingAnalyticsClient.GetTopCustomersAsync(
            topCustomerAssignment!.Top,
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
            && !assignment.RevenuePeriod.Equals("AllTime", StringComparison.OrdinalIgnoreCase)
            && !assignment.RevenuePeriod.Equals("Custom", StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("RevenuePeriod must be 'Month', 'Year', 'AllTime', or 'Custom'");
        }

        if (assignment.RevenuePeriod.Equals("Custom", StringComparison.OrdinalIgnoreCase))
        {
            if (!assignment.FromDate.HasValue || !assignment.ToDate.HasValue)
            {
                throw new Exception("FromDate and ToDate are required for Custom RevenuePeriod");
            }
            if (assignment.FromDate.Value > assignment.ToDate.Value)
            {
                throw new Exception("FromDate must be before or equal to ToDate");
            }
        }
    }

    private static (DateTime? From, DateTime? To) ResolveRevenuePeriod(TopCustomerVoucherAssignmentDTO assignment)
    {
        var now = DateTime.UtcNow;

        if (assignment.RevenuePeriod.Equals("Month", StringComparison.OrdinalIgnoreCase))
        {
            return (new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc), now);
        }

        if (assignment.RevenuePeriod.Equals("Year", StringComparison.OrdinalIgnoreCase))
        {
            return (new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc), now);
        }

        if (assignment.RevenuePeriod.Equals("AllTime", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        if (assignment.RevenuePeriod.Equals("Custom", StringComparison.OrdinalIgnoreCase))
        {
            return (assignment.FromDate, assignment.ToDate);
        }

        throw new Exception("RevenuePeriod must be 'Month', 'Year', 'AllTime', or 'Custom'");
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
            if (discountValue < 10000)
            {
                throw new Exception("Discount amount must be at least 10,000 VND");
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

    public async Task<bool> CheckBirthdayVoucherDistributedAsync(int month, int year)
    {
        var voucherCode = $"BDAY_{year}_{month:D2}";
        return await _voucherRepository.CodeExistsAsync(voucherCode);
    }

    public async Task<object> GetBirthdayPreviewAsync(int month, int year)
    {
        var voucherCode = $"BDAY_{year}_{month:D2}";
        var isDistributed = await _voucherRepository.CodeExistsAsync(voucherCode);
        var customers = await _userValidationService.GetCustomersByBirthdayMonthAsync(month);
        var activeCustomers = customers.Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)).ToList();

        return new
        {
            Month = month,
            Year = year,
            VoucherCode = voucherCode,
            IsDistributed = isDistributed,
            TotalEligibleCustomers = activeCustomers.Count,
            Customers = activeCustomers.Select(c => new
            {
                c.Id,
                c.FullName,
                c.Email,
                c.Status
            }).ToList()
        };
    }

    public async Task<object> DistributeBirthdayVoucherAsync(
        int month,
        int currentAdminId,
        string? discountType = "Percent",
        long? discountValue = 10,
        long? maxDiscountAmount = 500000,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var isVi = System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        var now = DateTime.Now;
        var currentMonth = now.Month;
        var nextMonth = currentMonth == 12 ? 1 : currentMonth + 1;

        if (month != currentMonth && month != nextMonth)
        {
            throw new Exception(isVi ? "Chỉ được phép phát voucher sinh nhật cho tháng hiện tại hoặc tháng kế tiếp." : "Birthday vouchers can only be distributed for the current or next month.");
        }

        var year = now.Year;
        if (month == 1 && currentMonth == 12)
        {
            year = now.Year + 1;
        }
        var voucherCode = $"BDAY_{year}_{month:D2}";

        if (await CheckBirthdayVoucherDistributedAsync(month, year))
        {
            throw new Exception(isVi ? $"Voucher sinh nhật cho tháng {month}/{year} đã được phát trước đó (Mã: {voucherCode})." : $"Birthday voucher for month {month}/{year} has already been distributed (Code: {voucherCode}).");
        }

        var customers = await _userValidationService.GetCustomersByBirthdayMonthAsync(month);
        
        var activeCustomers = customers.Where(c => string.Equals(c.Status, "Active", StringComparison.OrdinalIgnoreCase)).ToList();

        if (activeCustomers.Count == 0)
        {
            throw new Exception(isVi ? $"Không tìm thấy khách hàng nào hoạt động có sinh nhật trong tháng {month}." : $"No active customers found with a birthday in month {month}.");
        }

        var finalDiscountType = string.Equals(discountType, "Amount", StringComparison.OrdinalIgnoreCase) ? "Amount" : "Percent";
        var finalDiscountValue = discountValue.HasValue && discountValue.Value > 0 ? discountValue.Value : (finalDiscountType == "Percent" ? 10 : 50000);
        long? finalMaxDiscount = finalDiscountType == "Percent" ? (maxDiscountAmount.HasValue && maxDiscountAmount.Value > 0 ? maxDiscountAmount.Value : 500000) : null;

        var start = startDate ?? new DateTime(year, month, 1);
        var end = endDate ?? new DateTime(year, month, DateTime.DaysInMonth(year, month)).AddDays(30);

        if (start.Month != month)
        {
            throw new Exception(isVi ? $"Ngày bắt đầu ({start:dd/MM/yyyy}) phải thuộc Tháng sinh nhật được chọn (Tháng {month})." : $"Start date ({start:dd/MM/yyyy}) must be in the selected birthday month (Month {month}).");
        }

        if (end < start)
        {
            throw new Exception(isVi ? $"Ngày hết hạn ({end:dd/MM/yyyy}) phải lớn hơn hoặc bằng Ngày bắt đầu ({start:dd/MM/yyyy})." : $"Expiration date ({end:dd/MM/yyyy}) must be greater than or equal to Start date ({start:dd/MM/yyyy}).");
        }

        var description = isVi
            ? (finalDiscountType == "Percent"
                ? $"Chúc mừng sinh nhật! Giảm {finalDiscountValue}% (tối đa {finalMaxDiscount?.ToString("N0")} VNĐ) cho bất kỳ lượt đặt tour nào. Có hiệu lực trong tháng {month}."
                : $"Chúc mừng sinh nhật! Giảm {finalDiscountValue:N0} VNĐ cho bất kỳ lượt đặt tour nào. Có hiệu lực trong tháng {month}.")
            : (finalDiscountType == "Percent"
                ? $"Happy Birthday! Enjoy {finalDiscountValue}% off (up to {finalMaxDiscount?.ToString("N0")} VND) on any tour booking. Valid for month {month}."
                : $"Happy Birthday! Enjoy {finalDiscountValue:N0} VND off on any tour booking. Valid for month {month}.");

        var voucherDto = new CreateVoucherDTO
        {
            Code = voucherCode,
            DiscountType = finalDiscountType,
            DiscountValue = finalDiscountValue,
            MaxDiscountAmount = finalMaxDiscount,
            AvailableCount = activeCustomers.Count,
            StartDate = start,
            EndDate = end,
            Description = description,
            TourId = null, // Global voucher
            CustomerAssignments = activeCustomers.Select(c => new CreateUserVoucherAssignmentDTO
            {
                UserId = c.Id,
                Quantity = 1
            }).ToList()
        };

        var result = await Create(voucherDto, currentAdminId, true);

        // Send Emails
        int emailsSent = 0;
        foreach (var customer in activeCustomers)
        {
            try
            {
                var notifyTitle = isVi ? "🎁 Chúc mừng sinh nhật từ StayHub!" : "🎁 Happy Birthday from StayHub!";
                var notifyContent = isVi 
                    ? $"Chúng tôi đã gửi một mã giảm giá (Mã: {voucherCode}) vào tài khoản của bạn. Chúc bạn có một chuyến đi tuyệt vời!"
                    : $"We have sent a discount voucher (Code: {voucherCode}) to your account. Enjoy your trip!";
                await _notificationInternalService.NotifyUserAsync(customer.Id, notifyTitle, notifyContent);
            }
            catch
            {
                // Ignore notification failure
            }

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                try
                {
                    var subject = isVi ? "Chúc mừng sinh nhật từ StayHub!" : "Happy Birthday from StayHub!";
                    var body = isVi
                        ? $@"
                            <h3>Chúc mừng sinh nhật, {customer.FullName}!</h3>
                            <p>Chúng tôi rất vui mừng được chào đón tháng sinh nhật của bạn!</p>
                            <p>Đây là một món quà đặc biệt dành riêng cho bạn: một mã giảm giá (Mã: {voucherCode}) cho lần đặt tour tiếp theo.</p>
                            <p><strong>Mã giảm giá của bạn:</strong> {voucherCode}</p>
                            <p>Mã này đã được lưu sẵn vào ví voucher trong tài khoản của bạn. Chúc bạn có một hành trình đầy niềm vui!</p>"
                        : $@"
                            <h3>Happy Birthday, {customer.FullName}!</h3>
                            <p>We are excited to celebrate your birthday month with you!</p>
                            <p>Here is a special gift: a discount voucher (Code: {voucherCode}) for your next tour booking.</p>
                            <p><strong>Your Voucher Code:</strong> {voucherCode}</p>
                            <p>This voucher has already been saved to your account. Enjoy your trip!</p>";

                    await _emailService.SendEmailAsync(customer.Email, subject, body);
                    emailsSent++;
                }
                catch
                {
                    // Ignore email sending failures for individual customers
                }
            }
        }

        // Notify Admin
        var adminInfo = await _userValidationService.ValidateUserAsync(currentAdminId);
        if (adminInfo.Exists && !string.IsNullOrWhiteSpace(adminInfo.Email))
        {
            try
            {
                var adminSubject = isVi 
                    ? $"[StayHub Admin] Đã phát Voucher Sinh nhật - Tháng {month}/{year}" 
                    : $"[StayHub Admin] Birthday Vouchers Distributed - {month}/{year}";
                var adminBody = isVi
                    ? $@"
                        <h3>Báo cáo phát Voucher Sinh nhật</h3>
                        <p>Các voucher sinh nhật của tháng {month}/{year} đã được phát thành công.</p>
                        <p><strong>Mã Voucher:</strong> {voucherCode}</p>
                        <p><strong>Tổng số khách hàng đủ điều kiện:</strong> {activeCustomers.Count}</p>
                        <p><strong>Tổng số email đã gửi:</strong> {emailsSent}</p>
                        <p>Hành động được thực hiện bởi Admin ID: {currentAdminId}</p>"
                    : $@"
                        <h3>Birthday Vouchers Report</h3>
                        <p>The birthday vouchers for {month}/{year} have been successfully distributed.</p>
                        <p><strong>Voucher Code:</strong> {voucherCode}</p>
                        <p><strong>Total Eligible Customers:</strong> {activeCustomers.Count}</p>
                        <p><strong>Total Emails Sent:</strong> {emailsSent}</p>
                        <p>Action performed by Admin ID: {currentAdminId}</p>";

                await _emailService.SendEmailAsync(adminInfo.Email, adminSubject, adminBody);
            }
            catch
            {
                // Ignore admin email error
            }
        }

        return new
        {
            VoucherCode = voucherCode,
            TotalEligibleCustomers = activeCustomers.Count,
            EmailsSent = emailsSent,
            Message = isVi ? "Đã phát voucher sinh nhật thành công." : "Birthday vouchers distributed successfully."
        };
    }

    public async Task<bool> DeleteBirthdayVoucherAsync(int month, int year)
    {
        var isVi = System.Globalization.CultureInfo.CurrentCulture.Name.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        var voucherCode = $"BDAY_{year}_{month:D2}";
        var voucher = await _voucherRepository.GetByCodeAsync(voucherCode);
        if (voucher == null)
        {
            throw new Exception(isVi ? "Không tìm thấy voucher sinh nhật của tháng này." : "Birthday voucher for this month not found.");
        }

        if (voucher.StartDate <= DateTime.Now)
        {
            throw new Exception(isVi ? "Voucher sinh nhật này đã bắt đầu thời hạn sử dụng, không thể hủy." : "This birthday voucher has already started and cannot be cancelled.");
        }

        await _voucherRepository.DeleteAsync(voucher);
        return true;
    }

    private static string ResolveStatus(Voucher voucher)
    {
        var now = DateTime.UtcNow;
        if (now > voucher.EndDate) return "Expired";
        if (now < voucher.StartDate) return "Scheduled";
        if (voucher.UsedCount >= voucher.AvailableCount && voucher.AvailableCount > 0) return "Depleted";
        if (!voucher.IsActive) return "Inactive";
        return "Active";
    }
}
