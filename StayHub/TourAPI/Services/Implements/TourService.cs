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
            ValidateAndNormalize(model);

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
            ValidateAndNormalize(model);

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

        public async Task<PaginationDTO<ReadTourDTO>> GetAll(int page, int pageSize, int userId, bool isAdmin, string? searchTerm = null, int? categoryId = null)
        {
            var result = await _repository.GetPagedAsync(new TourQueryOptions
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                CategoryId = categoryId
            });

            var list = _mapper.Map<List<ReadTourDTO>>(result.Items);
            foreach (var tour in list)
            {
                tour.CanEdit = isAdmin || tour.CreatedBy == userId;
            }

            return CreatePagination(list, result.Total, page, pageSize);
        }

        public async Task<PaginationDTO<ReadTourDTO>> GetActiveTours(int page, int pageSize)
        {
            var result = await _repository.GetPagedAsync(new TourQueryOptions
            {
                Page = page,
                PageSize = pageSize,
                ActiveOnly = true
            });

            var list = _mapper.Map<List<ReadTourDTO>>(result.Items);
            return CreatePagination(list, result.Total, page, pageSize);
        }

        public async Task<PaginationDTO<ReadTourDTO>> SearchTours(int page, int pageSize, string? searchTerm = null, int? categoryId = null, string? country = null, string? city = null, long? minPrice = null, long? maxPrice = null, DateTime? startDate = null, DateTime? endDate = null, int? duration = null, string? sortBy = null)
        {
            var result = await _repository.GetPagedAsync(new TourQueryOptions
            {
                Page = page,
                PageSize = pageSize,
                ActiveOnly = true,
                SearchTerm = searchTerm,
                CategoryId = categoryId,
                Country = country,
                City = city,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                StartDate = startDate,
                EndDate = endDate,
                Duration = duration,
                SortBy = sortBy
            });

            var list = _mapper.Map<List<ReadTourDTO>>(result.Items);

            var allReviews = list.Where(t => t.Reviews != null).SelectMany(t => t.Reviews!).ToList();
            if (allReviews.Any())
            {
                await PopulateReviewerNamesAsync(allReviews);
            }

            return CreatePagination(list, result.Total, page, pageSize);
        }

        public async Task<ReadTourDTO> GetById(int id, int? userId = null, bool isAdmin = false)
        {
            var entity = await _repository.GetById(id);
            var tourDto = _mapper.Map<ReadTourDTO>(entity);

            if (tourDto != null)
            {
                tourDto.CanEdit = isAdmin ||
                    (userId.HasValue && entity.CreatedBy == userId.Value);

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
            var result = await _repository.GetPagedAsync(new TourQueryOptions
            {
                Page = page,
                PageSize = pageSize,
                SearchTerm = searchTerm,
                SortDescendingById = true
            });

            var list = _mapper.Map<List<ReadTourDTO>>(result.Items);
            var allReviews = list.Where(t => t.Reviews != null).SelectMany(t => t.Reviews!).ToList();
            if (allReviews.Any())
            {
                await PopulateReviewerNamesAsync(allReviews);
            }

            return CreatePagination(list, result.Total, page, pageSize);
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

        public async Task<List<ItineraryLocationDto>> GetItinerariesByTourIdAsync(int tourId)
        {
            var tour = await _repository.GetById(tourId);
            if (tour == null)
            {
                throw new Exception("Tour not found.");
            }

            var query = tour.TourItineraries?.AsEnumerable() ?? Enumerable.Empty<TourItinerary>();
            var result = query.Where(x => x.TourId == tourId)
                              .OrderBy(x => x.DayNumber)
                              .ThenBy(x => x.StartDuration).ToList();
            return _mapper.Map<List<ItineraryLocationDto>>(result);
        }

        private static PaginationDTO<T> CreatePagination<T>(
            List<T> data,
            int total,
            int page,
            int pageSize)
        {
            var normalizedPage = Math.Max(1, page);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 1000);

            return new PaginationDTO<T>
            {
                Data = data,
                CurrentPage = normalizedPage,
                PageSize = normalizedPageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)normalizedPageSize)
            };
        }

        private static void ValidateAndNormalize(BaseTourDTO model)
        {
            model.Name = model.Name.Trim();
            model.Description = model.Description?.Trim();
            model.Country = model.Country?.Trim();
            model.City = model.City?.Trim();
            model.Address = model.Address?.Trim();

            if (model.Image != null)
            {
                const long maxImageBytes = 5 * 1024 * 1024;
                var allowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "image/jpeg", "image/png", "image/webp"
                };

                if (model.Image.Length == 0 || model.Image.Length > maxImageBytes)
                {
                    throw new Exception("Tour image must be a non-empty file no larger than 5 MB.");
                }

                if (!allowedContentTypes.Contains(model.Image.ContentType))
                {
                    throw new Exception("Tour image must be a JPEG, PNG, or WebP file.");
                }
            }
        }
    }
}
