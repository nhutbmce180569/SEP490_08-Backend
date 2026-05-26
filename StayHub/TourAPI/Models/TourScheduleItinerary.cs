using System;
using System.Collections.Generic;

namespace TourAPI.Models;

public partial class TourScheduleItinerary
{
    public int Id { get; set; }

    public int ScheduleId { get; set; }

    public int DayNumber { get; set; }

    public DateTime ItineraryDate { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public TimeOnly? StartDuration { get; set; }

    public TimeOnly? EndDuration { get; set; }

    public string? LocationName { get; set; }

    public double? LocationLat { get; set; }

    public double? LocationLng { get; set; }

    public int? TourismInfoId { get; set; }

    public virtual TourSchedule Schedule { get; set; } = null!;
}
