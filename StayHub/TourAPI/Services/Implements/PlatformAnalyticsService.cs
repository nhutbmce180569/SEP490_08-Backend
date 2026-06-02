using TourAPI.DTOs;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class PlatformAnalyticsService : IPlatformAnalyticsService
    {
        private readonly IPlatformAnalyticsClients _clients;
        private readonly IPlatformCatalogRepository _catalogRepository;

        public PlatformAnalyticsService(
            IPlatformAnalyticsClients clients,
            IPlatformCatalogRepository catalogRepository)
        {
            _clients = clients;
            _catalogRepository = catalogRepository;
        }

        public async Task<PlatformAnalyticsOverviewDTO> GetOverviewAsync(DateTime? from, DateTime? to)
        {
            var range = ResolveDateRange(from, to);

            var users = await _clients.GetUserStatsAsync(range.From, range.To, "day")
                ?? throw new Exception("Unable to retrieve platform user stats from Identity service.");

            var operations = await _clients.GetOperationsStatsAsync(range.From, range.To);
            var catalog = await _catalogRepository.GetCatalogStatsAsync(5);
            var vouchers = await _clients.GetVoucherStatsAsync();
            var social = await _clients.GetSocialStatsAsync();

            return new PlatformAnalyticsOverviewDTO
            {
                PeriodFrom = range.From,
                PeriodTo = range.To,
                TotalUsers = users.TotalUsers,
                ActiveUsers = users.ActiveUsers,
                NewUsersInPeriod = users.NewUsersInPeriod,
                TotalManagers = users.TotalManagers,
                TotalStaff = users.TotalStaff,
                TotalTours = catalog.TotalTours,
                ActiveTours = catalog.ActiveTours,
                TotalSchedules = catalog.TotalSchedules,
                UpcomingSchedules = catalog.UpcomingSchedules,
                ScheduleOccupancyRate = catalog.ScheduleOccupancyRate,
                ActiveVouchers = vouchers?.ActiveVouchers ?? 0,
                TotalTourMoments = social?.TotalTourMoments ?? 0,
                TotalChatRooms = social?.TotalChatRooms ?? 0,
                CheckInRate = operations?.CheckInRate ?? 0,
                PendingCancellationRequests = operations?.PendingCancellationRequests ?? 0
            };
        }

        public async Task<PlatformUserAnalyticsDTO> GetUsersAsync(DateTime? from, DateTime? to)
        {
            var range = ResolveDateRange(from, to);

            var users = await _clients.GetUserStatsAsync(range.From, range.To, "day")
                ?? throw new Exception("Unable to retrieve platform user stats from Identity service.");

            return new PlatformUserAnalyticsDTO
            {
                TotalUsers = users.TotalUsers,
                ActiveUsers = users.ActiveUsers,
                InactiveUsers = users.InactiveUsers,
                NewUsersInPeriod = users.NewUsersInPeriod,
                OnlineRecently = users.OnlineRecently,
                TotalCustomers = users.TotalCustomers,
                TotalManagers = users.TotalManagers,
                TotalStaff = users.TotalStaff,
                TotalAdmins = users.TotalAdmins,
                ByRole = users.ByRole ?? []
            };
        }

        public async Task<PlatformCatalogAnalyticsDTO> GetCatalogAsync(int top)
        {
            ValidateTop(top);
            return await _catalogRepository.GetCatalogStatsAsync(top);
        }

        public async Task<PlatformVoucherAnalyticsDTO> GetVouchersAsync()
        {
            var vouchers = await _clients.GetVoucherStatsAsync()
                ?? throw new Exception("Unable to retrieve platform voucher stats from Voucher service.");

            return new PlatformVoucherAnalyticsDTO
            {
                TotalVouchers = vouchers.TotalVouchers,
                ActiveVouchers = vouchers.ActiveVouchers,
                ExpiredVouchers = vouchers.ExpiredVouchers,
                TotalRedemptions = vouchers.TotalRedemptions,
                RedemptionRate = vouchers.RedemptionRate,
                ByDiscountType = vouchers.ByDiscountType ?? [],
                ByUserVoucherStatus = vouchers.ByUserVoucherStatus ?? []
            };
        }

        public async Task<PlatformSocialAnalyticsDTO> GetSocialAsync()
        {
            var social = await _clients.GetSocialStatsAsync()
                ?? throw new Exception("Unable to retrieve platform social stats from Social service.");

            return new PlatformSocialAnalyticsDTO
            {
                TotalFriendships = social.TotalFriendships,
                AcceptedFriendships = social.AcceptedFriendships,
                PendingFriendRequests = social.PendingFriendRequests,
                TotalChatRooms = social.TotalChatRooms,
                GroupChatRooms = social.GroupChatRooms,
                TotalChatMessages = social.TotalChatMessages,
                UnreadChatMessages = social.UnreadChatMessages,
                TotalTourMoments = social.TotalTourMoments,
                TotalMomentReactions = social.TotalMomentReactions,
                TotalMomentComments = social.TotalMomentComments,
                MomentsByPrivacy = social.MomentsByPrivacy ?? [],
                FriendshipStatusDistribution = social.FriendshipStatusDistribution ?? []
            };
        }

        public async Task<PlatformHealthAnalyticsDTO> GetHealthAsync(DateTime? from, DateTime? to)
        {
            var range = ResolveDateRange(from, to);

            var operations = await _clients.GetOperationsStatsAsync(range.From, range.To);
            var catalog = await _catalogRepository.GetCatalogStatsAsync(5);
            var vouchers = await _clients.GetVoucherStatsAsync();
            var reviewResponseRate = await _catalogRepository.GetReviewResponseRateAsync();

            var occupancy = catalog.ScheduleOccupancyRate;
            var checkIn = operations?.CheckInRate ?? 0;
            var voucherRate = vouchers?.RedemptionRate ?? 0;
            var pendingCancellations = operations?.PendingCancellationRequests ?? 0;

            var indicators = new List<PlatformHealthIndicatorDTO>
            {
                BuildIndicator("Schedule occupancy", occupancy, 60, 40),
                BuildIndicator("Ticket check-in", checkIn, 80, 60),
                BuildIndicator("Review response", reviewResponseRate, 50, 30),
                BuildIndicator("Voucher redemption", voucherRate, 30, 15)
            };

            if (pendingCancellations > 5)
            {
                indicators.Add(new PlatformHealthIndicatorDTO
                {
                    Name = "Pending cancellations",
                    Value = pendingCancellations,
                    Unit = "count",
                    Status = pendingCancellations > 10 ? "Warning" : "Fair"
                });
            }

            var warningCount = indicators.Count(i => i.Status is "Warning" or "Fair");
            var criticalCount = indicators.Count(i => i.Status == "Critical");

            var overall = criticalCount > 0 ? "Critical"
                : warningCount >= 2 ? "NeedsAttention"
                : warningCount > 0 ? "Fair"
                : "Healthy";

            return new PlatformHealthAnalyticsDTO
            {
                ScheduleOccupancyRate = occupancy,
                CheckInRate = checkIn,
                ReviewResponseRate = reviewResponseRate,
                VoucherRedemptionRate = voucherRate,
                PendingCancellationRequests = pendingCancellations,
                OverallStatus = overall,
                Indicators = indicators
            };
        }

        private static PlatformHealthIndicatorDTO BuildIndicator(
            string name,
            decimal value,
            decimal goodThreshold,
            decimal warningThreshold,
            bool invert = false)
        {
            string status;
            if (invert)
            {
                status = value <= goodThreshold ? "Good"
                    : value <= warningThreshold ? "Warning"
                    : "Critical";
            }
            else
            {
                status = value >= goodThreshold ? "Good"
                    : value >= warningThreshold ? "Warning"
                    : "Critical";
            }

            return new PlatformHealthIndicatorDTO
            {
                Name = name,
                Value = value,
                Status = status
            };
        }

        private static (DateTime From, DateTime To) ResolveDateRange(DateTime? from, DateTime? to)
        {
            var resolvedTo = to ?? DateTime.UtcNow;
            var resolvedFrom = from ?? resolvedTo.AddDays(-30);

            if (resolvedFrom > resolvedTo)
            {
                throw new ArgumentException("Start date must be before or equal to end date.");
            }

            if ((resolvedTo - resolvedFrom).TotalDays > 366)
            {
                throw new ArgumentException("Date range cannot exceed 366 days.");
            }

            return (resolvedFrom, resolvedTo);
        }

        private static void ValidateTop(int top)
        {
            if (top <= 0 || top > 20)
            {
                throw new ArgumentException("Top must be between 1 and 20.");
            }
        }
    }
}
