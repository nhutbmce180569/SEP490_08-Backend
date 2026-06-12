namespace TourAPI.DTOs
{
    public class TourRouteDto
    {
        public int ScheduleId { get; set; }
        public string? TourName { get; set; }
        public List<WaypointDto> Waypoints { get; set; } = new List<WaypointDto>();
        public List<List<double>> GeometryCoordinates { get; set; } = new List<List<double>>();
    }

    public class WaypointDto
    {
        public string? Name { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int Sequence { get; set; }
    }
}