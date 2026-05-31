using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BookingAPI.DTOs;
using BookingAPI.Repositories; // Thêm namespace Repository Order của bạn

namespace BookingAPI.Services.Implements
{
    public class EligibleScheduleService : IEligibleScheduleService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly HttpClient _httpClient;

        public EligibleScheduleService(IOrderRepository orderRepository, HttpClient httpClient)
        {
            _orderRepository = orderRepository;
            _httpClient = httpClient;
        }

        public async Task<List<EligibleScheduleDto>> GetEligibleSchedulesAsync(int userId)
        {
            // 1. Query lấy danh sách ScheduleId hợp lệ từ Orders (Status 'Paid' hoặc 'Completed')
            // Bạn cần chuẩn bị sẵn hàm này ở OrderRepository
            var scheduleIds = await _orderRepository.GetEligibleScheduleIdsByUserIdAsync(userId);

            if (scheduleIds == null || !scheduleIds.Any())
            {
                return new List<EligibleScheduleDto>(); // Không có chuyến đi nào
            }

            // Loại bỏ các ID trùng lặp đề phòng mua nhiều vé cùng 1 tour
            var distinctScheduleIds = scheduleIds.Distinct().ToList();

            // 2. Dùng HttpClient gọi sang CatalogAPI (TourAPI) lấy thông tin Tour
            HttpResponseMessage response;
            try
            {
                // Thay đổi URL Gateway/Catalog tùy thuộc vào hệ thống của bạn
                var catalogApiUrl = "https://localhost:7010/api/tourschedules/batch";
                response = await _httpClient.PostAsJsonAsync(catalogApiUrl, distinctScheduleIds);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to connect to CatalogAPI: {ex.Message}", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"CatalogAPI returned error status: {response.StatusCode}");
            }

            var scheduleDetails = await response.Content.ReadFromJsonAsync<List<TourScheduleDetailDto>>();

            if (scheduleDetails == null || !scheduleDetails.Any())
            {
                return new List<EligibleScheduleDto>();
            }

            // 3. Xử lý Map dữ liệu & gắn StatusContext
            var now = DateTime.Now.Date;
            var resultList = new List<EligibleScheduleDto>();

            foreach (var schedule in scheduleDetails)
            {
                string statusContext;

                if (now >= schedule.DepartureDate.Date && now <= schedule.ReturnDate.Date)
                {
                    statusContext = "Ongoing";
                }
                else if (now < schedule.DepartureDate.Date)
                {
                    statusContext = "Upcoming";
                }
                else
                {
                    statusContext = "Completed";
                }

                resultList.Add(new EligibleScheduleDto
                {
                    ScheduleId = schedule.Id,
                    TourName = schedule.Tour?.Name ?? "Unknown Tour",
                    DepartureDate = schedule.DepartureDate,
                    ReturnDate = schedule.ReturnDate,
                    StatusContext = statusContext
                });
            }

            // 4. SẮP XẾP LUỒNG (QUAN TRỌNG)
            var sortedResult = resultList
                // Ưu tiên theo nhóm: Ongoing(1) -> Upcoming(2) -> Completed(3)
                .OrderBy(x => x.StatusContext == "Ongoing" ? 1 : (x.StatusContext == "Upcoming" ? 2 : 3))
                // Ưu tiên 2: Upcoming (Sắp diễn ra xếp trước -> Tăng dần theo DepartureDate)
                .ThenBy(x => x.StatusContext == "Upcoming" ? x.DepartureDate : DateTime.MaxValue)
                // Ưu tiên 3: Completed (Đã kết thúc xếp cuối, nhưng MỚI NHẤT xếp trước -> Giảm dần ReturnDate)
                .ThenByDescending(x => x.StatusContext == "Completed" ? x.ReturnDate : DateTime.MinValue)
                .ToList();

            return sortedResult;
        }
    }
}
