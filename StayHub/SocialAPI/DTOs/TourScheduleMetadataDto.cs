using System;
using System.Collections.Generic;

namespace SocialAPI.DTOs
{
    public class TourScheduleMetadataDto
    {
        public int ScheduleId { get; set; }
        public int TourId { get; set; }
        public int TourCreatedBy { get; set; }
        public List<int> StaffIds { get; set; } = new();
        public DateTime DepartureDate { get; set; }
        public DateTime ReturnDate { get; set; }
    }
}
