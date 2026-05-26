using System;
using System.Collections.Generic;

namespace TourAPI.Models;

public partial class TourSchedule
{
    public int Id { get; set; }

    public int TourId { get; set; }

    public DateTime DepartureDate { get; set; }

    public DateTime ReturnDate { get; set; }

    public long Price { get; set; }

    public int MaxCapacity { get; set; }

    public int? SoldQuantity { get; set; }

    public int AvailableSeats { get; set; }

    public string? Note { get; set; }

    public virtual Tour Tour { get; set; } = null!;

    public virtual ICollection<TourScheduleItinerary> TourScheduleItineraries { get; set; } = new List<TourScheduleItinerary>();

    public virtual ICollection<TourScheduleStaff> TourScheduleStaffs { get; set; } = new List<TourScheduleStaff>();
}
