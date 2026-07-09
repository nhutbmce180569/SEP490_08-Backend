using System.ComponentModel.DataAnnotations;

namespace TourAPI.DTOs
{
    public class ChangeTourManagerRequest
    {
        [Required]
        public int ManagerId { get; set; }
    }
}
