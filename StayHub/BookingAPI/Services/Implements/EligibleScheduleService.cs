using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BookingAPI.DTOs;
using BookingAPI.Repositories; // Thêm namespace Repository Order của bạn
using Microsoft.AspNetCore.Http;

namespace BookingAPI.Services.Implements
{
    public class EligibleScheduleService : IEligibleScheduleService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public EligibleScheduleService(IOrderRepository orderRepository, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _orderRepository = orderRepository;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
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
            var scheduleDetails = new List<TourScheduleDetailDto>();
            var tourNameDictionary = new Dictionary<int, string>();

            // 2. Dùng HttpClient gọi sang CatalogAPI (TourAPI) lấy thông tin Tour
            try
            {
                var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
                var client = _httpClientFactory.CreateClient();

                if (!string.IsNullOrWhiteSpace(token))
                {
                    client.DefaultRequestHeaders.Add("Authorization", token);
                }

                // Thay đổi URL Gateway/Catalog tùy thuộc vào hệ thống của bạn
                var catalogApiUrl = "http://localhost:5046/api/tourschedules/batch";
                var scheduleResponse = await client.PostAsJsonAsync(catalogApiUrl, distinctScheduleIds);

                if (scheduleResponse.IsSuccessStatusCode)
                {
                    var fetchedDetails = await scheduleResponse.Content.ReadFromJsonAsync<List<TourScheduleDetailDto>>();
                    if (fetchedDetails != null)
                    {
                        scheduleDetails = fetchedDetails;

                        var tourIds = scheduleDetails.Where(s => s.TourId > 0).Select(s => s.TourId).Distinct().ToList();
                        if (tourIds.Any())
                        {
                            var toursApiUrl = "http://localhost:5046/api/tours/batch";
                            var toursResponse = await client.PostAsJsonAsync(toursApiUrl, tourIds);

                            if (toursResponse.IsSuccessStatusCode)
                            {
                                var fetchedTours = await toursResponse.Content.ReadFromJsonAsync<List<TourDetailFromApiDto>>();
                                if (fetchedTours != null)
                                {
                                    // Bảo vệ chống sập khi trùng Key bằng GroupBy, đề phòng một TourId bị API trả về nhiều lần
                                    tourNameDictionary = fetchedTours
                                        .Where(t => t.Id > 0)
                                        .GroupBy(t => t.Id)
                                        .ToDictionary(g => g.Key, g => g.First().Name ?? "StayHub Tour");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Fallback: Bỏ qua lỗi ngầm để thực hiện fallback dữ liệu mặc định bên dưới
            }

            // 3. Xử lý Map dữ liệu & gắn StatusContext
            var now = DateTime.Now.Date;
            var resultList = new List<EligibleScheduleDto>();

            if (scheduleDetails.Any())
            {
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

                    tourNameDictionary.TryGetValue(schedule.TourId, out var realTourName);

                    var baseTourName = !string.IsNullOrWhiteSpace(realTourName) ? realTourName : "StayHub Tour";

                    resultList.Add(new EligibleScheduleDto
                    {
                        ScheduleId = schedule.Id,
                        TourName = $"{baseTourName} - {schedule.DepartureDate:dd/MM/yyyy}",
                        DepartureDate = schedule.DepartureDate,
                        ReturnDate = schedule.ReturnDate,
                        StatusContext = statusContext
                    });
                }
            }
            else
            {
                // Nếu API lấy chi tiết lỗi, gán chuỗi an toàn mặc định cho danh sách ID
                foreach (var scheduleId in distinctScheduleIds)
                {
                    resultList.Add(new EligibleScheduleDto
                    {
                        ScheduleId = scheduleId,
                        TourName = "StayHub Tour",
                        DepartureDate = DateTime.MinValue,
                        ReturnDate = DateTime.MinValue,
                        StatusContext = "Unknown"
                    });
                }
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
