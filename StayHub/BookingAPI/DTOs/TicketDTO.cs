﻿using System;
using System.ComponentModel.DataAnnotations;

namespace BookingAPI.DTOs
{
    public class ReadTicketDTO
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public int OrderDetailId { get; set; }

        public int? UserId { get; set; }

        public int TicketTypeId { get; set; }

        public string AttendeeName { get; set; } = null!;

        public string IdCard { get; set; } = null!;

        public DateOnly? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Nationality { get; set; }

        public string? QrCode { get; set; }

        public string? CheckInStatus { get; set; }
    }
    public abstract class BaseTicketDTO
    {
        public int? UserId { get; set; }

        public int? TicketTypeId { get; set; }

        public string AttendeeName { get; set; } = null!;

        public string IdCard { get; set; } = null!;

        public DateOnly? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Nationality { get; set; }

        public string? QrCode { get; set; }

        public string? CheckInStatus { get; set; }
    }

    public class CreateTicketDTO : BaseTicketDTO
    {

    }
    public class UpdateTicketDTO : BaseTicketDTO
    {

    }

    public class CheckInRequestDTO
    {
        public string QrCode { get; set; } = string.Empty;

        public int ScheduleId { get; set; }
    }
}
