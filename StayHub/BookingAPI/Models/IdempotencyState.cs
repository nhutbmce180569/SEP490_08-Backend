using System;

namespace BookingAPI.Models
{
    public class IdempotencyState
    {
        public string Status { get; set; } = "Processing"; // Processing or Completed
        public int? OrderId { get; set; }
    }
}
