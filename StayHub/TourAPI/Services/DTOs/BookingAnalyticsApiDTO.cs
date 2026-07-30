namespace VoucherAPI.DTOs;

public class BookingTopCustomersResponse
{
    public string? Message { get; set; }
    public List<BookingTopCustomer>? Data { get; set; }
}

public class BookingTopCustomer
{
    public int CustomerId { get; set; }
    public long TotalSpend { get; set; }
    public int OrderCount { get; set; }
    public DateTime? LastOrderAt { get; set; }
    public int TotalTickets { get; set; }
}
