using System.ComponentModel.DataAnnotations;

namespace VoucherAPI.DTOs;

public class ReadVoucherDTO
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int? TourId { get; set; }

    public string? TourName { get; set; }

    public string DiscountType { get; set; } = null!;

    public long DiscountValue { get; set; }

    public long? MaxDiscountAmount { get; set; }

    public int UsedCount { get; set; }

    public int AvailableCount { get; set; }

    public int RemainingCount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public string? Description { get; set; }

    public int CreatorId { get; set; }

    public string? CreatorName { get; set; }

    public bool IsActive { get; set; }

    public string Status { get; set; } = null!;

    public bool IsCustomerSpecific { get; set; }

    public int AssignedCustomerCount { get; set; }
}

public class ReadVoucherDetailDTO : ReadVoucherDTO
{
    public List<ReadUserVoucherDTO> AssignedCustomers { get; set; } = new();
}

public class ReadUserVoucherDTO
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? UserFullName { get; set; }

    public string? UserEmail { get; set; }

    public int Quantity { get; set; }

    public string Status { get; set; } = null!;
}

public abstract class BaseVoucherDTO
{
    [Required(ErrorMessage = "Code is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Code must be between 3 and 50 characters")]
    [RegularExpression(@"^[A-Za-z0-9_-]+$", ErrorMessage = "Code must contain only letters, numbers, hyphens, and underscores")]
    public string Code { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "TourId must be greater than 0 when provided")]
    public int? TourId { get; set; }

    [Required(ErrorMessage = "DiscountType is required")]
    [RegularExpression(@"^(Percent|Amount)$", ErrorMessage = "DiscountType must be 'Percent' or 'Amount'")]
    public string DiscountType { get; set; } = null!;

    [Range(1, long.MaxValue, ErrorMessage = "DiscountValue must be greater than 0")]
    public long DiscountValue { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "MaxDiscountAmount must be greater than 0")]
    public long? MaxDiscountAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AvailableCount must be at least 1")]
    public int AvailableCount { get; set; }

    [Required(ErrorMessage = "StartDate is required")]
    public DateTime StartDate { get; set; }

    [Required(ErrorMessage = "EndDate is required")]
    public DateTime EndDate { get; set; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }
}

public class CreateVoucherDTO : BaseVoucherDTO
{
    public List<CreateUserVoucherAssignmentDTO>? CustomerAssignments { get; set; }

    public TopCustomerVoucherAssignmentDTO? TopCustomerAssignment { get; set; }
}

public class CreateUserVoucherAssignmentDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "UserId must be greater than 0")]
    public int UserId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;
}

public class TopCustomerVoucherAssignmentDTO
{
    [Range(1, 100, ErrorMessage = "Top must be between 1 and 100")]
    public int Top { get; set; }

    [Required(ErrorMessage = "RevenuePeriod is required")]
    [RegularExpression(@"^(Month|Year|AllTime)$", ErrorMessage = "RevenuePeriod must be 'Month', 'Year', or 'AllTime'")]
    public string RevenuePeriod { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;
}

public class UpdateVoucherDTO
{
    [Range(1, int.MaxValue, ErrorMessage = "TourId must be greater than 0 when provided")]
    public int? TourId { get; set; }

    [RegularExpression(@"^(Percent|Amount)$", ErrorMessage = "DiscountType must be 'Percent' or 'Amount'")]
    public string? DiscountType { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "DiscountValue must be greater than 0")]
    public long? DiscountValue { get; set; }

    [Range(1, long.MaxValue, ErrorMessage = "MaxDiscountAmount must be greater than 0")]
    public long? MaxDiscountAmount { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "AvailableCount must be at least 1")]
    public int? AvailableCount { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    public List<CreateUserVoucherAssignmentDTO>? CustomerAssignments { get; set; }

    public TopCustomerVoucherAssignmentDTO? TopCustomerAssignment { get; set; }
}

public class UserApiResponse
{
    public string? Message { get; set; }

    public ReadUserApiDTO? Data { get; set; }
}

public class ReadUserApiDTO
{
    public int Id { get; set; }

    public string? Email { get; set; }

    public string? FullName { get; set; }

    public string? Status { get; set; }
}

public class TourApiResponse
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Status { get; set; }
}
