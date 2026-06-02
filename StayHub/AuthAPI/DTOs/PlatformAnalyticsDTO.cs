namespace AuthAPI.DTOs
{
    public class PlatformUserStatsDTO
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int NewUsersInPeriod { get; set; }
        public int OnlineRecently { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalManagers { get; set; }
        public int TotalStaff { get; set; }
        public int TotalAdmins { get; set; }
        public List<LabelCountDTO> ByRole { get; set; } = [];
    }
}
