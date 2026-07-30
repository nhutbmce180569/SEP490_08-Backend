using TourAPI.DTOs;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class CustomerAnalyticsService : ICustomerAnalyticsService
    {
        private readonly IAuthAnalyticsClient _authClient;
        private readonly IBookingAnalyticsClient _bookingClient;
        private readonly ICustomerEngagementRepository _engagementRepository;

        public CustomerAnalyticsService(
            IAuthAnalyticsClient authClient,
            IBookingAnalyticsClient bookingClient,
            ICustomerEngagementRepository engagementRepository)
        {
            _authClient = authClient;
            _bookingClient = bookingClient;
            _engagementRepository = engagementRepository;
        }

        public async Task<CustomerAnalyticsOverviewDTO> GetOverviewAsync(DateTime? from, DateTime? to)
        {
            var range = ResolveDateRange(from, to);
            var demographics = await _authClient.GetDemographicsAsync(range.From, range.To, "day")
                ?? throw new Exception("Unable to retrieve customer demographics from Identity service.");

            var orders = await _bookingClient.GetOverviewAsync(range.From, range.To)
                ?? throw new Exception("Unable to retrieve order analytics from Booking service.");

            var engagement = await _engagementRepository.GetEngagementOverviewAsync(demographics.TotalCustomers);
            var segments = await _bookingClient.GetSegmentsAsync(range.From, range.To, demographics.TotalCustomers)
                ?? new BookingSegmentsData();

            return new CustomerAnalyticsOverviewDTO
            {
                PeriodFrom = range.From,
                PeriodTo = range.To,
                TotalCustomers = demographics.TotalCustomers,
                ActiveCustomers = demographics.ActiveCustomers,
                NewCustomersInPeriod = demographics.NewCustomersInPeriod,
                TotalOrders = orders.TotalOrders,
                PaidOrders = orders.PaidOrders,
                CompletedOrders = orders.CompletedOrders,
                PendingOrders = orders.PendingOrders,
                CancelledOrders = orders.CancelledOrders,
                TotalRevenue = orders.TotalRevenue,
                AverageOrderValue = orders.AverageOrderValue,
                UniqueBuyers = orders.UniqueCustomersWithOrders,
                RepeatCustomers = orders.RepeatCustomers,
                RepeatCustomerRate = orders.RepeatCustomerRate,
                BuyerConversionRate = segments.BuyerConversionRate,
                CancellationRate = orders.CancellationRate,
                TotalRefundAmount = orders.TotalRefundAmount,
                TotalReviews = engagement.TotalReviews,
                UniqueReviewers = engagement.UniqueReviewers,
                AverageReviewRating = engagement.AverageRating,
                TotalWishlists = engagement.TotalWishlists,
                UniqueWishlistCustomers = engagement.UniqueWishlistCustomers,
                ReviewParticipationRate = engagement.ReviewParticipationRate,
                WishlistToBuyerRate = engagement.UniqueWishlistCustomers > 0
                    ? Math.Round(orders.UniqueCustomersWithOrders * 100m / engagement.UniqueWishlistCustomers, 2)
                    : 0
            };
        }

        public async Task<CustomerDemographicsAnalyticsDTO> GetDemographicsAsync(DateTime? from, DateTime? to)
        {
            var range = ResolveDateRange(from, to);
            var data = await _authClient.GetDemographicsAsync(range.From, range.To, "day")
                ?? throw new Exception("Unable to retrieve customer demographics from Identity service.");

            return new CustomerDemographicsAnalyticsDTO
            {
                TotalCustomers = data.TotalCustomers,
                ActiveCustomers = data.ActiveCustomers,
                InactiveCustomers = data.InactiveCustomers,
                NewCustomersInPeriod = data.NewCustomersInPeriod,
                ByGender = data.ByGender ?? [],
                ByProvider = data.ByProvider ?? [],
                ByStatus = data.ByStatus ?? [],
                ByAgeGroup = data.ByAgeGroup ?? []
            };
        }

        public async Task<CustomerSegmentAnalyticsDTO> GetSegmentsAsync(DateTime? from, DateTime? to)
        {
            var range = ResolveDateRange(from, to);
            var demographics = await _authClient.GetDemographicsAsync(range.From, range.To, "day")
                ?? throw new Exception("Unable to retrieve customer demographics from Identity service.");

            var segments = await _bookingClient.GetSegmentsAsync(range.From, range.To, demographics.TotalCustomers)
                ?? throw new Exception("Unable to retrieve order segments from Booking service.");

            var orders = await _bookingClient.GetOverviewAsync(range.From, range.To)
                ?? new BookingOverviewData();

            var engagement = await _engagementRepository.GetEngagementOverviewAsync(demographics.TotalCustomers);

            var orderStatusDistribution = new List<AnalyticsLabelCountDTO>();
            if (orders.TotalOrders > 0)
            {
                orderStatusDistribution =
                [
                    BuildDistribution("Paid", orders.PaidOrders, orders.TotalOrders),
                    BuildDistribution("Completed", orders.CompletedOrders, orders.TotalOrders),
                    BuildDistribution("Pending", orders.PendingOrders, orders.TotalOrders),
                    BuildDistribution("Cancelled", orders.CancelledOrders, orders.TotalOrders)
                ];
            }

            return new CustomerSegmentAnalyticsDTO
            {
                NeverPurchased = segments.NeverPurchased,
                OneTimeBuyers = segments.OneTimeBuyers,
                RepeatBuyers = segments.RepeatBuyers,
                NewBuyersInPeriod = segments.NewBuyersInPeriod,
                AtRiskCustomers = segments.AtRiskCustomers,
                HighValueCustomers = segments.HighValueCustomers,
                BuyerConversionRate = segments.BuyerConversionRate,
                OrderStatusDistribution = orderStatusDistribution,
                RatingDistribution = engagement.RatingDistribution
            };
        }

        public async Task<CustomerTrendAnalyticsDTO> GetTrendsAsync(
            DateTime? from,
            DateTime? to,
            string granularity)
        {
            var range = ResolveDateRange(from, to);
            granularity = NormalizeGranularity(granularity);

            var demographics = await _authClient.GetDemographicsAsync(range.From, range.To, granularity)
                ?? throw new Exception("Unable to retrieve customer demographics from Identity service.");

            var orderTrends = await _bookingClient.GetTrendsAsync(range.From, range.To, granularity);

            return new CustomerTrendAnalyticsDTO
            {
                PeriodFrom = range.From,
                PeriodTo = range.To,
                Granularity = granularity,
                RegistrationTrend = demographics.RegistrationTrend ?? [],
                OrderTrend = orderTrends.Select(t => new CustomerTrendPointDTO
                {
                    Period = t.Period,
                    Count = t.OrderCount,
                    UniqueCustomers = t.UniqueCustomers
                }).ToList(),
                RevenueTrend = orderTrends.Select(t => new CustomerTrendPointDTO
                {
                    Period = t.Period,
                    Count = t.OrderCount,
                    Amount = t.Revenue,
                    UniqueCustomers = t.UniqueCustomers
                }).ToList()
            };
        }

        public async Task<List<TopCustomerAnalyticsDTO>> GetTopCustomersAsync(
            int top,
            DateTime? from,
            DateTime? to)
        {
            ValidateTop(top);
            var range = ResolveDateRange(from, to);

            var topCustomers = await _bookingClient.GetTopCustomersAsync(top, range.From, range.To);
            if (topCustomers.Count == 0) return [];

            var customerIds = topCustomers.Select(x => x.CustomerId).ToList();
            var authCustomers = (await _authClient.GetCustomersBatchAsync(customerIds))
                .ToDictionary(c => c.Id);

            var reviewCounts = await _engagementRepository.GetReviewCountsByCustomerAsync();
            var wishlistCounts = await _engagementRepository.GetWishlistCountsByCustomerAsync();
            var avgRatings = await _engagementRepository.GetAverageRatingsByCustomerAsync();

            return topCustomers.Select(tc =>
            {
                authCustomers.TryGetValue(tc.CustomerId, out var customer);
                reviewCounts.TryGetValue(tc.CustomerId, out var reviewCount);
                wishlistCounts.TryGetValue(tc.CustomerId, out var wishlistCount);
                avgRatings.TryGetValue(tc.CustomerId, out var avgRating);

                return new TopCustomerAnalyticsDTO
                {
                    CustomerId = tc.CustomerId,
                    FullName = customer?.FullName ?? $"Customer #{tc.CustomerId}",
                    Email = customer?.Email ?? string.Empty,
                    AvatarUrl = customer?.AvatarUrl,
                    TotalSpend = tc.TotalSpend,
                    OrderCount = tc.OrderCount,
                    TotalTickets = tc.TotalTickets,
                    LastOrderAt = tc.LastOrderAt,
                    ReviewCount = reviewCount,
                    WishlistCount = wishlistCount,
                    AverageRatingGiven = avgRating > 0 ? avgRating : null
                };
            }).ToList();
        }

        public async Task<CustomerEngagementAnalyticsDTO> GetEngagementAsync()
        {
            var demographics = await _authClient.GetDemographicsAsync(null, null, "day")
                ?? throw new Exception("Unable to retrieve customer demographics from Identity service.");

            return await _engagementRepository.GetEngagementOverviewAsync(demographics.TotalCustomers);
        }

        public async Task<PaginationDTO<CustomerListItemAnalyticsDTO>> GetCustomerListAsync(
            CustomerListQueryDTO query)
        {
            ValidateListQuery(query);
            var range = ResolveDateRange(query.From, query.To);

            var authList = await _authClient.GetCustomerListAsync(query.Search, query.Page, query.PageSize)
                ?? throw new Exception("Unable to retrieve customer list from Identity service.");

            var customers = authList.Customers ?? [];
            var customerIds = customers.Select(c => c.Id).ToList();

            var orderMetrics = customerIds.Count > 0
                ? await _bookingClient.GetCustomerMetricsAsync(customerIds, range.From, range.To)
                : [];

            var metricsByCustomer = orderMetrics.ToDictionary(m => m.CustomerId);
            var reviewCounts = await _engagementRepository.GetReviewCountsByCustomerAsync();
            var wishlistCounts = await _engagementRepository.GetWishlistCountsByCustomerAsync();

            var items = customers.Select(c =>
            {
                metricsByCustomer.TryGetValue(c.Id, out var metrics);
                reviewCounts.TryGetValue(c.Id, out var reviewCount);
                wishlistCounts.TryGetValue(c.Id, out var wishlistCount);

                return new CustomerListItemAnalyticsDTO
                {
                    CustomerId = c.Id,
                    FullName = c.FullName,
                    Email = c.Email,
                    AvatarUrl = c.AvatarUrl,
                    Status = c.Status,
                    Provider = c.Provider,
                    CreatedAt = c.CreatedAt,
                    LastOnline = c.LastOnline,
                    TotalOrders = metrics?.TotalOrders ?? 0,
                    TotalSpend = metrics?.TotalSpend ?? 0,
                    ReviewCount = reviewCount,
                    WishlistCount = wishlistCount,
                    LastOrderAt = metrics?.LastOrderAt,
                    CustomerSegment = ResolveSegment(metrics)
                };
            }).ToList();

            items = ApplySorting(items, query.SortBy, query.SortOrder);

            return new PaginationDTO<CustomerListItemAnalyticsDTO>
            {
                Data = items,
                Total = authList.Total,
                TotalPages = (int)Math.Ceiling(authList.Total / (double)query.PageSize),
                CurrentPage = query.Page,
                PageSize = query.PageSize
            };
        }

        public async Task<CustomerDetailAnalyticsDTO?> GetCustomerDetailAsync(
            int customerId,
            DateTime? from,
            DateTime? to)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException("Invalid customer ID.");
            }

            var range = ResolveDateRange(from, to);
            var customer = await _authClient.GetCustomerSummaryAsync(customerId);
            if (customer == null) return null;

            var metrics = await _bookingClient.GetCustomerMetricsByIdAsync(customerId, range.From, range.To);
            var reviewCounts = await _engagementRepository.GetReviewCountsByCustomerAsync();
            var wishlistCounts = await _engagementRepository.GetWishlistCountsByCustomerAsync();
            var avgRatings = await _engagementRepository.GetAverageRatingsByCustomerAsync();

            reviewCounts.TryGetValue(customerId, out var reviewCount);
            wishlistCounts.TryGetValue(customerId, out var wishlistCount);
            avgRatings.TryGetValue(customerId, out var avgRating);

            var demographics = await _authClient.GetDemographicsAsync(null, null, "day");
            var segments = demographics != null
                ? await _bookingClient.GetSegmentsAsync(range.From, range.To, demographics.TotalCustomers)
                : null;

            var atRiskThreshold = DateTime.UtcNow.AddDays(-90);
            var isAtRisk = metrics?.LastOrderAt.HasValue == true && metrics.LastOrderAt.Value < atRiskThreshold;

            return new CustomerDetailAnalyticsDTO
            {
                CustomerId = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                AvatarUrl = customer.AvatarUrl,
                PhoneNumber = null,
                Gender = customer.Gender,
                DateOfBirth = customer.DateOfBirth,
                Provider = customer.Provider,
                Status = customer.Status,
                CreatedAt = customer.CreatedAt,
                LastOnline = customer.LastOnline,
                TotalOrders = metrics?.TotalOrders ?? 0,
                PaidOrders = metrics?.PaidOrders ?? 0,
                PendingOrders = metrics?.PendingOrders ?? 0,
                CancelledOrders = metrics?.CancelledOrders ?? 0,
                TotalSpend = metrics?.TotalSpend ?? 0,
                AverageOrderValue = metrics?.AverageOrderValue ?? 0,
                TotalTickets = metrics?.TotalTickets ?? 0,
                FirstOrderAt = metrics?.FirstOrderAt,
                LastOrderAt = metrics?.LastOrderAt,
                ReviewCount = reviewCount,
                AverageRatingGiven = avgRating > 0 ? avgRating : null,
                WishlistCount = wishlistCount,
                CustomerSegment = ResolveSegment(metrics),
                IsAtRisk = isAtRisk,
                IsHighValue = segments?.HighValueCustomers > 0 && (metrics?.TotalSpend ?? 0) > 0 &&
                              metrics!.PaidOrders >= 2
            };
        }

        private static (DateTime From, DateTime To) ResolveDateRange(DateTime? from, DateTime? to)
        {
            var resolvedTo = to ?? DateTime.UtcNow;
            var resolvedFrom = from ?? resolvedTo.AddYears(-10);

            if (resolvedFrom > resolvedTo)
            {
                throw new ArgumentException("Start date must be before or equal to end date.");
            }

            if ((resolvedTo - resolvedFrom).TotalDays > 3650)
            {
                throw new ArgumentException("Date range cannot exceed 10 years.");
            }

            return (resolvedFrom, resolvedTo);
        }

        private static void ValidateTop(int top)
        {
            if (top <= 0 || top > 1000)
            {
                throw new ArgumentException("Top must be between 1 and 1000.");
            }
        }

        private static void ValidateListQuery(CustomerListQueryDTO query)
        {
            if (query.Page <= 0) query.Page = 1;
            if (query.PageSize <= 0 || query.PageSize > 100) query.PageSize = 20;
            query.SortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "totalSpend" : query.SortBy;
            query.SortOrder = string.IsNullOrWhiteSpace(query.SortOrder) ? "desc" : query.SortOrder.ToLowerInvariant();
        }

        private static string NormalizeGranularity(string granularity)
        {
            var normalized = (granularity ?? "day").Trim().ToLowerInvariant();
            return normalized is "day" or "week" or "month" ? normalized : "day";
        }

        private static AnalyticsLabelCountDTO BuildDistribution(string label, int count, int total)
        {
            return new AnalyticsLabelCountDTO
            {
                Label = label,
                Count = count,
                Percentage = total > 0 ? Math.Round(count * 100m / total, 2) : 0
            };
        }

        private static string ResolveSegment(BookingCustomerMetrics? metrics)
        {
            if (metrics == null || metrics.PaidOrders == 0)
            {
                return "NeverPurchased";
            }

            if (metrics.PaidOrders == 1)
            {
                return "OneTimeBuyer";
            }

            return "RepeatBuyer";
        }

        private static List<CustomerListItemAnalyticsDTO> ApplySorting(
            List<CustomerListItemAnalyticsDTO> items,
            string sortBy,
            string sortOrder)
        {
            var descending = sortOrder == "desc";

            return sortBy switch
            {
                "orderCount" => descending
                    ? items.OrderByDescending(x => x.TotalOrders).ToList()
                    : items.OrderBy(x => x.TotalOrders).ToList(),
                "reviewCount" => descending
                    ? items.OrderByDescending(x => x.ReviewCount).ToList()
                    : items.OrderBy(x => x.ReviewCount).ToList(),
                "wishlistCount" => descending
                    ? items.OrderByDescending(x => x.WishlistCount).ToList()
                    : items.OrderBy(x => x.WishlistCount).ToList(),
                "createdAt" => descending
                    ? items.OrderByDescending(x => x.CreatedAt).ToList()
                    : items.OrderBy(x => x.CreatedAt).ToList(),
                "lastOrderAt" => descending
                    ? items.OrderByDescending(x => x.LastOrderAt).ToList()
                    : items.OrderBy(x => x.LastOrderAt).ToList(),
                _ => descending
                    ? items.OrderByDescending(x => x.TotalSpend).ToList()
                    : items.OrderBy(x => x.TotalSpend).ToList()
            };
        }
    }
}
