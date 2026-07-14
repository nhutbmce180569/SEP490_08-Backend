using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class ReadTourScheduleTicketDTO
    {
        public int Id { get; set; }

        public int ScheduleId { get; set; }

        public int TicketTypeId { get; set; }

        public long Price { get; set; }

        public int Quantity { get; set; }

        public int? SoldQuantity { get; set; }

        public int AvailableQuantity { get; set; }

        public bool? IsActive { get; set; }

        public string? Note { get; set; }

        public ReadPromotionDTO? Promotion { get; set; }
    }

    public abstract class BaseTourScheduleTicketDTO
    {
        [Required(ErrorMessage = "ScheduleId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "ScheduleId must be greater than 0")]
        public int ScheduleId { get; set; }

        [Required(ErrorMessage = "TicketTypeId is required")]
        [Range(1, int.MaxValue, ErrorMessage = "TicketTypeId must be greater than 0")]
        public int TicketTypeId { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, long.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        public long Price { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be greater than or equal to 0")]
        public int Quantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "SoldQuantity must be greater than or equal to 0")]
        public int? SoldQuantity { get; set; }

        public bool? IsActive { get; set; }

        public string? Note { get; set; }

        public int? PromotionId { get; set; }
    }

    public class CreateTourScheduleTicketDTO : BaseTourScheduleTicketDTO { }

    public class UpdateTourScheduleTicketDTO : BaseTourScheduleTicketDTO { }

    public class UpdateTourScheduleTicketQuantityDTO
    {
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Quantity { get; set; }
    }
}
