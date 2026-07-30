namespace AIAPI.Clients;

public class PaginationResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
}

public class ExternalTourDTO
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string? ImageUrl { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public string? Status { get; set; }
    public double? AverageStar { get; set; }
    public List<ExternalTourItineraryDTO>? TourItineraries { get; set; }
    public List<ExternalTourScheduleDTO>? TourSchedules { get; set; }
    public List<ExternalReviewDTO>? Reviews { get; set; }
}

public class ExternalTourItineraryDTO
{
    public int DayNumber { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? TourismInfoId { get; set; }
    public double? LocationLat { get; set; }
    public double? LocationLng { get; set; }
}

public class ExternalTourScheduleDTO
{
    public DateTime DepartureDate { get; set; }
    public DateTime ReturnDate { get; set; }
    public List<ExternalTourScheduleTicketDTO>? TourScheduleTickets { get; set; }
}

public class ExternalTourScheduleTicketDTO
{
    public long Price { get; set; }
    public int AvailableQuantity { get; set; }
    public bool? IsActive { get; set; }
}

public class ExternalReviewDTO
{
    public int Rating { get; set; }
}

public class ExternalTourismInformationDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Description { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public string? Status { get; set; }
}

public class ExternalWishlistItemDTO
{
    public int TourId { get; set; }
    public string TourName { get; set; } = "";
}

public class BookingOrdersWrapperDTO
{
    public string? Message { get; set; }
    public PaginationResponse<ExternalOrderDTO>? Data { get; set; }
}

public class ExternalOrderDTO
{
    public int Id { get; set; }
    public string? Status { get; set; }
    public ExternalOrderTourDTO? Tour { get; set; }
    public ExternalOrderScheduleDTO? Schedule { get; set; }
}

public class ExternalOrderTourDTO
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public string Name { get; set; } = "";
    public string? City { get; set; }
    public string? Country { get; set; }
}

public class ExternalOrderScheduleDTO
{
    public int TourId { get; set; }
}

public class ExternalCategoryDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Slug { get; set; } = "";
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
