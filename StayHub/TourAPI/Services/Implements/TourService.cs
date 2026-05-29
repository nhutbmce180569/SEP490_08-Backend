using AutoMapper;
using Humanizer;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class TourService : ITourService
    {
        private readonly ITourRepository _repository;
        private readonly ICategoryService _categoryService;
        private readonly IOrderService _bookingService;

        private readonly CloudinaryService _cloudinary;
        private readonly HttpClient _httpClient;
        IMapper _mapper;

        public TourService(IOrderService bookingService, ICategoryService categoryService, ITourRepository repository, IMapper mapper, CloudinaryService cloudinary, HttpClient httpClient)
        {
            _repository = repository;
            _mapper = mapper;
            _cloudinary = cloudinary;
            _httpClient = httpClient;
            _categoryService = categoryService;
            _bookingService = bookingService;
        }
        public async Task Add(CreateTourDTO model, int createdBy)
        {
            var checkCategory =
                await _categoryService.CheckCategoryExist(model.CategoryId);

            if (!checkCategory)
                throw new Exception("Category does not exist");

            var tour = _mapper.Map<Tour>(model);

            tour.Status = "Inactive";
            tour.CreatedAt = DateTime.Now;
            tour.CreatedBy = createdBy;
            await _repository.Add(tour);

            await _repository.SaveChangesAsync();

            if (model.Image != null)
            {
                var imageUrl = await _cloudinary.UploadImageAsync(
                    model.Image,
                    "tours",
                    "tour",
                    tour.Id
                );

                tour.ImageUrl = imageUrl;
            }

            await _repository.SaveChangesAsync();
        }

        public async Task Update(int id, UpdateTourDTO model, int updatedBy)
        {

            var checkCategory = await _categoryService.CheckCategoryExist(model.CategoryId);

            if (!checkCategory)
                throw new Exception("Category does not exist");

            var tour = await _repository.GetById(id);

            if (tour == null)
            {
                throw new Exception("Not found");
            }

            if (tour.Status == "Active")
            {
                throw new Exception("Please inactive tour before edit");
            }

            _mapper.Map(model, tour);

            if (model.RemoveImage)
            {
                if (!string.IsNullOrEmpty(tour.ImageUrl))
                {
                    await _cloudinary.DeleteImageAsync($"tour_{id}");
                    tour.ImageUrl = null;
                }
            }
            else if (model.Image != null)
            {
                var imageUrl = await _cloudinary.UploadImageAsync(
                    model.Image,
                    "tours",
                    "tour",
                    tour.Id
                );

                tour.ImageUrl = imageUrl;
            }

            tour.UpdatedAt = DateTime.Now;
            tour.UpdatedBy = updatedBy;
            _repository.Update(tour);
            await _repository.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var tour = await _repository.GetById(id);

            if (tour == null)
            {
                throw new Exception("Not found");
            }

            var scheduleIds = tour.TourSchedules?.Select(s => s.Id).ToList() ?? new List<int>();
            var request = new CheckBookingTour()
            {
                ScheduleIds = scheduleIds,
            };
            var hasBooking = await _bookingService.CheckTourHasOrder(request);

            if (hasBooking)
                throw new Exception("Cannot delete tour because there are existing bookings.");

            if (tour.ImageUrl != null)
            {
                await _cloudinary.DeleteImageAsync($"tour_{id}");
            }
            await _repository.Delete(id);
        }

        public async Task<PaginationDTO<ReadTourDTO>> GetAll(int page, int pageSize)
        {
            var list = _mapper.Map<List<ReadTourDTO>>(await _repository.GetAll());
            int total = list.Count;

            list = list
                    .OrderBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();


            var tours = new PaginationDTO<ReadTourDTO>
            {
                Data = list,
                CurrentPage = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
            return tours;
        }

        public async Task<PaginationDTO<ReadTourDTO>> GetActiveTours(int page, int pageSize)
        {
            var list = _mapper.Map<List<ReadTourDTO>>(await _repository.GetActiveTours());
            int total = list.Count;

            list = list
                    .OrderBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();


            var tours = new PaginationDTO<ReadTourDTO>
            {
                Data = list,
                CurrentPage = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
            return tours;
        }

        public async Task<PaginationDTO<ReadTourDTO>> SearchTours(int page, int pageSize, string? searchTerm = null, int? categoryId = null, string? country = null, string? city = null, long? minPrice = null, long? maxPrice = null, DateTime? startDate = null, DateTime? endDate = null, int? duration = null, string? sortBy = null)
        {
            var tours = await _repository.GetActiveTours();
            var query = tours.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(t =>
                    (t.Name != null && t.Name.ToLower().Contains(term)) ||
                    (t.Description != null && t.Description.ToLower().Contains(term)) ||
                    (t.TourItineraries != null && t.TourItineraries.Any(i => (i.Title != null && i.Title.ToLower().Contains(term)) || (i.Description != null && i.Description.ToLower().Contains(term))))
                );
            }

            if (categoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(country))
            {
                query = query.Where(t => t.Country != null && t.Country.Equals(country, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                query = query.Where(t => t.City != null && t.City.Equals(city, StringComparison.OrdinalIgnoreCase));
            }

            //if (minPrice.HasValue || maxPrice.HasValue)
            //{
            //    query = query.Where(t => t.TourSchedules != null && t.TourSchedules.Any(s =>
            //        (!minPrice.HasValue || s.Price >= minPrice.Value) &&
            //        (!maxPrice.HasValue || s.Price <= maxPrice.Value)
            //    ));
            //}

            if (startDate.HasValue || endDate.HasValue)
            {
                query = query.Where(t => t.TourSchedules != null && t.TourSchedules.Any(s =>
                    (!startDate.HasValue || s.DepartureDate.Date >= startDate.Value.Date) &&
                    (!endDate.HasValue || s.ReturnDate.Date <= endDate.Value.Date)
                ));
            }

            if (duration.HasValue)
            {
                query = query.Where(t =>
                    (t.TourItineraries != null && t.TourItineraries.Count == duration.Value) ||
                    (t.TourSchedules != null && t.TourSchedules.Any(s =>
                        (s.ReturnDate.Date - s.DepartureDate.Date).Days == duration.Value ||
                        (s.ReturnDate.Date - s.DepartureDate.Date).Days + 1 == duration.Value
                    ))
                );
            }

            //if (!string.IsNullOrWhiteSpace(sortBy))
            //{
            //    switch (sortBy.ToLower())
            //    {
            //        case "price_asc":
            //            query = query.OrderBy(t => t.TourSchedules != null && t.TourSchedules.Any() ? t.TourSchedules.Min(s => s.Price) : long.MaxValue);
            //            break;
            //        case "price_desc":
            //            query = query.OrderByDescending(t => t.TourSchedules != null && t.TourSchedules.Any() ? t.TourSchedules.Min(s => s.Price) : 0);
            //            break;
            //        case "date_asc":
            //            query = query.OrderBy(t => t.TourSchedules != null && t.TourSchedules.Any() ? t.TourSchedules.Min(s => s.DepartureDate) : DateTime.MaxValue);
            //            break;
            //        case "date_desc":
            //            query = query.OrderByDescending(t => t.TourSchedules != null && t.TourSchedules.Any() ? t.TourSchedules.Min(s => s.DepartureDate) : DateTime.MinValue);
            //            break;
            //        default:
            //            query = query.OrderBy(t => t.Id);
            //            break;
            //    }
            //}
            //else
            //{
            //    query = query.OrderBy(t => t.Id);
            //}

            var filteredTours = query.ToList();
            int total = filteredTours.Count;

            var pagedTours = filteredTours
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

            var list = _mapper.Map<List<ReadTourDTO>>(pagedTours);

            // GỌI HÀM LẮP TÊN NGƯỜI DÙNG CHO TOÀN BỘ REVIEW TRONG TRANG NÀY
            var allReviews = list.Where(t => t.Reviews != null).SelectMany(t => t.Reviews!).ToList();
            if (allReviews.Any())
            {
                await PopulateReviewerNamesAsync(allReviews);
            }

            var result = new PaginationDTO<ReadTourDTO>
            {
                Data = list,
                CurrentPage = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
            return result;
        }

        public async Task<ReadTourDTO> GetById(int id)
        {
            var tourDto = _mapper.Map<ReadTourDTO>(await _repository.GetById(id));

            if (tourDto != null)
            {
                // Lấy tên Operator cho 1 tour này
                //await PopulateOperatorNamesAsync(new List<ReadTourDTO> { tourDto });
                tourDto.CreatedByName = await GetAccountNameByIdAsync(tourDto.CreatedBy);
                if (tourDto.UpdatedBy != null)
                {
                    tourDto.UpdatedByName = await GetAccountNameByIdAsync((int)tourDto.UpdatedBy);
                }
                // Lấy tên Reviewer
                if (tourDto.Reviews != null && tourDto.Reviews.Any())
                {
                    await PopulateReviewerNamesAsync(tourDto.Reviews);
                }
            }

            return tourDto;
        }
        public async Task<ReadTourDTO> GetActiveTour(int id)
        {
            var entity = await _repository.GetById(id);

            if (entity == null)
            {
                throw new Exception("Tour not found");
            }

            var tour = _mapper.Map<ReadTourDTO>(entity);

            if (tour.Status == "Inactive")
            {
                throw new Exception("This tour is inactive");
            }

            if (tour.Reviews != null && tour.Reviews.Any())
            {
                tour.Reviews = tour.Reviews
                    .OrderByDescending(r => r.Id)
                    .Take(5)
                    .OrderBy(r => r.Rating)
                    .ToList();

                await PopulateReviewerNamesAsync(tour.Reviews);
            }

            return tour;
        }

        public async Task ActiveTour(int id, bool isActive)
        {
            var tour = await _repository.GetById(id);

            if (tour == null)
            {
                throw new Exception("Not found");
            }

            if (isActive)
            {
                tour.Status = "Active";
            }
            else
            {
                tour.Status = "Inactive";
            }
            _repository.Update(tour);
            await _repository.SaveChangesAsync();
        }

        public async Task UpdateTourStatusAsync(int id, string status)
        {
            var tour = await _repository.GetById(id);

            if (tour == null)
            {
                throw new Exception("Tour not found");
            }

            // Gán status mới (Active, Inactive, Pending, Banned...)
            tour.Status = status;

            _repository.Update(tour);
            await _repository.SaveChangesAsync();
        }

        public async Task<PaginationDTO<ReadTourDTO>> GetByAdmin(int page, int pageSize, string? searchTerm = null)
        {
            // 1. Lấy toàn bộ tour từ DB
            var tours = await _repository.GetAll();
            var query = tours.AsEnumerable();

            // 2. Tìm kiếm (nếu có)
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(t =>
                    (t.Name != null && t.Name.ToLower().Contains(term)) ||
                    (t.Description != null && t.Description.ToLower().Contains(term)) ||
                    (t.TourItineraries != null && t.TourItineraries.Any(i => (i.Title != null && i.Title.ToLower().Contains(term)) || (i.Description != null && i.Description.ToLower().Contains(term))))
                );
            }

            // 3. Sắp xếp (Admin thường muốn xem tour mới tạo nhất đưa lên đầu)
            query = query.OrderByDescending(t => t.Id);

            var filteredTours = query.ToList();
            int total = filteredTours.Count;

            // 4. Phân trang
            var pagedTours = filteredTours
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

            // 5. Map sang DTO
            var list = _mapper.Map<List<ReadTourDTO>>(pagedTours);
            //await PopulateOperatorNamesAsync(list);

            // 6. GỌI HÀM LẮP TÊN NGƯỜI DÙNG CHO TOÀN BỘ REVIEW (Giống như hàm GetActiveTours)
            var allReviews = list.Where(t => t.Reviews != null).SelectMany(t => t.Reviews!).ToList();
            if (allReviews.Any())
            {
                await PopulateReviewerNamesAsync(allReviews);
            }

            // 7. Trả về kết quả
            var result = new PaginationDTO<ReadTourDTO>
            {
                Data = list,
                CurrentPage = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize)
            };
            return result;
        }

        private async Task PopulateReviewerNamesAsync(IEnumerable<ReadReviewDTO> reviews)
        {
            if (reviews == null || !reviews.Any()) return;

            var uniqueCustomerIds = reviews.Select(r => r.CustomerId).Distinct().ToList();

            var gatewayUrl = "https://localhost:7010";

            var userDict = new ConcurrentDictionary<int, (string Name, string? Avatar)>();

            var tasks = uniqueCustomerIds.Select(async id =>
            {
                try
                {
                    var response = await _httpClient.GetAsync($"{gatewayUrl}/api/users/{id}");
                    if (response.IsSuccessStatusCode)
                    {
                        var apiResponse = await response.Content.ReadFromJsonAsync<UserApiResponse>();

                        var fullName = apiResponse?.Data?.FullName ?? "Unknown User";
                        var avatar = apiResponse?.Data?.AvatarUrl;

                        userDict.TryAdd(id, (fullName, avatar));
                    }
                    else
                    {
                        userDict.TryAdd(id, ("Unknown User", null));
                    }
                }
                catch
                {
                    userDict.TryAdd(id, ("Unknown User", null));
                }
            });

            await Task.WhenAll(tasks);

            foreach (var review in reviews)
            {
                if (userDict.TryGetValue(review.CustomerId, out var userInfo))
                {
                    review.CustomerName = userInfo.Name;
                    review.CustomerAvatar = userInfo.Avatar;
                }
            }
        }

        // Hàm Helper lấy tên Operator từ AuthAPI
        private async Task<string> GetAccountNameByIdAsync(int id)
        {
            // Lấy danh sách OperatorId duy nhất (tránh gọi API trùng lặp)
            //            var uniqueOperatorIds = tours.Select(t => t.OperatorId).Distinct().ToList();

            var gatewayUrl = "https://localhost:7010"; // Cổng Gateway của bạn


            var response = await _httpClient.GetAsync($"{gatewayUrl}/api/users/{id}");
            string name = "";
            if (response.IsSuccessStatusCode)
            {
                var apiResponse = await response.Content.ReadFromJsonAsync<UserApiResponse>();
                name = apiResponse?.Data?.FullName;
            }
            return name;
        }
        public async Task<int> CountToursByCategoryIdAsync(int categoryId)
        {
            return await _repository.CountByCategoryIdAsync(categoryId);
        }
    }
}
