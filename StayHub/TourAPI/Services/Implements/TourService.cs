using AutoMapper;
using Humanizer;
using Microsoft.Extensions.Configuration;
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
        private readonly IEmailService _emailService;

        private readonly CloudinaryService _cloudinary;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        IMapper _mapper;

        public TourService(IOrderService bookingService, ICategoryService categoryService, ITourRepository repository, IMapper mapper, CloudinaryService cloudinary, HttpClient httpClient, IConfiguration configuration, IEmailService emailService)
        {
            _repository = repository;
            _mapper = mapper;
            _cloudinary = cloudinary;
            _httpClient = httpClient;
            _configuration = configuration;
            _categoryService = categoryService;
            _bookingService = bookingService;
            _emailService = emailService;
        }
        public async Task Add(CreateTourDTO model, int createdBy)
        {
            ValidateAndNormalize(model);

            var isDuplicate = await _repository.IsNameDuplicateAsync(model.Name);
            if (isDuplicate)
                throw new Exception("Tour name already exists.");

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

            if (model.TourImages != null && model.TourImages.Any())
            {
                var tourImages = new List<TourImage>();
                foreach (var file in model.TourImages)
                {
                    var fileUrl = await _cloudinary.UploadGalleryImageAsync(
                        file,
                        "tours/gallery",
                        $"tour_gallery_{tour.Id}_{Guid.NewGuid()}"
                    );
                    if (fileUrl != null)
                    {
                        tourImages.Add(new TourImage { TourId = tour.Id, ImageUrl = fileUrl });
                    }
                }
                if (tourImages.Any())
                {
                    await _repository.AddTourImages(tourImages);
                }
            }

            await _repository.SaveChangesAsync();
        }

        public async Task Update(int id, UpdateTourDTO model, int updatedBy)
        {
            ValidateAndNormalize(model);

            var isDuplicate = await _repository.IsNameDuplicateAsync(model.Name, id);
            if (isDuplicate)
                throw new Exception("Tour name already exists.");

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

            if (model.RemovedTourImageIds != null && model.RemovedTourImageIds.Any())
            {
                var imagesToRemove = await _repository.GetTourImagesByIds(model.RemovedTourImageIds);
                // Optionally delete from Cloudinary if we parse the public_id, but DB remove is enough for now
                if (imagesToRemove.Any())
                {
                    _repository.RemoveTourImages(imagesToRemove);
                }
            }

            if (model.TourImages != null && model.TourImages.Any())
            {
                var newTourImages = new List<TourImage>();
                foreach (var file in model.TourImages)
                {
                    var fileUrl = await _cloudinary.UploadGalleryImageAsync(
                        file,
                        "tours/gallery",
                        $"tour_gallery_{tour.Id}_{Guid.NewGuid()}"
                    );
                    if (fileUrl != null)
                    {
                        newTourImages.Add(new TourImage { TourId = tour.Id, ImageUrl = fileUrl });
                    }
                }
                if (newTourImages.Any())
                {
                    await _repository.AddTourImages(newTourImages);
                }
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

            if (tour.Status != null && !tour.Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Cannot delete tour: Tour must be deactivated (Inactive) first.");
            }

            var scheduleIds = tour.TourSchedules?.Select(s => s.Id).ToList() ?? new List<int>();
            if (scheduleIds.Any())
            {
                throw new Exception("Cannot delete tour: Tour has existing schedules. Please delete all schedules first.");
            }

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

        public async Task<PaginationDTO<ReadTourDTO>> GetAll(int page, int pageSize, int userId, bool isAdmin, string? searchTerm = null, int? categoryId = null, bool createdByMe = false)
        {
            var result = await _repository.GetAll(
                page,
                pageSize,
                searchTerm,
                categoryId,
                createdByMe ? userId : null);

            var list = _mapper.Map<List<ReadTourDTO>>(result.Tours);
            foreach (var tour in list)
            {
                tour.CanEdit = isAdmin || tour.CreatedBy == userId;
            }

            return CreatePagination(list, result.Total, page, pageSize);
        }

        public async Task<PaginationDTO<ReadTourDTO>> GetActiveTours(int page, int pageSize)
        {
            var result = await _repository.GetActiveTours(page, pageSize);
            var list = _mapper.Map<List<ReadTourDTO>>(result.Tours);

            return CreatePagination(list, result.Total, page, pageSize);
        }

        public async Task<PaginationDTO<ReadTourDTO>> SearchTours(int page, int pageSize, string? searchTerm = null, int? categoryId = null, string? country = null, string? city = null, long? minPrice = null, long? maxPrice = null, DateTime? startDate = null, DateTime? endDate = null, int? duration = null, string? sortBy = null)
        {
            var result = await _repository.SearchTours(
                page,
                pageSize,
                searchTerm,
                categoryId,
                country,
                city,
                minPrice,
                maxPrice,
                startDate,
                endDate,
                duration,
                sortBy);

            var list = _mapper.Map<List<ReadTourDTO>>(result.Tours);

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

        public async Task<PaginationDTO<ReadTourDTO>> GetByAdmin(int page, int pageSize, string? searchTerm = null, int? managerId = null)
        {
            var result = await _repository.GetByAdmin(page, pageSize, searchTerm, managerId);
            var list = _mapper.Map<List<ReadTourDTO>>(result.Tours);

            return CreatePagination(list, result.Total, page, pageSize);
        }


        public async Task<PaginationDTO<ReadTourDTO>> GetByManager(int managerId, int page, int pageSize, string? searchTerm = null)
        {
            var result = await _repository.GetByManager(
                managerId,
                page,
                pageSize,
                searchTerm);
            var list = _mapper.Map<List<ReadTourDTO>>(result.Tours);
            foreach (var tour in list)
            {
                tour.CanEdit = true;
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

        public async Task<IEnumerable<ReadTourDTO>> GetToursByIdsAsync(IEnumerable<int> tourIds)
        {
            if (tourIds == null || !tourIds.Any())
            {
                return Enumerable.Empty<ReadTourDTO>();
            }

            var distinctIds = tourIds.Distinct().ToList();
            var tours = new List<Tour>();

            foreach (var id in distinctIds)
            {
                var tour = await _repository.GetById(id);
                if (tour != null)
                {
                    tours.Add(tour);
                }
            }

            return _mapper.Map<List<ReadTourDTO>>(tours);
        }

        public async Task<PaginationDTO<ReadTourDTO>> GetSaleTours(int page, int pageSize)
        {
            var result = await _repository.GetSaleTours(page, pageSize);
            var list = _mapper.Map<List<ReadTourDTO>>(result.Items);
            return CreatePagination(list, result.TotalCount, page, pageSize);
        }

        public async Task<PaginationDTO<ReadTourDTO>> GetHotTours(int page, int pageSize)
        {
            var result = await _repository.GetHotTours(page, pageSize);
            var list = _mapper.Map<List<ReadTourDTO>>(result.Items);
            return CreatePagination(list, result.TotalCount, page, pageSize);
        }

        public async Task<PaginationDTO<ReadTourDTO>> GetUpcomingTours(int page, int pageSize)
        {
            var result = await _repository.GetUpcomingTours(page, pageSize);
            var list = _mapper.Map<List<ReadTourDTO>>(result.Items);
            return CreatePagination(list, result.TotalCount, page, pageSize);
        }

        public async Task<PaginationDTO<ReadTourDTO>> GetToursByRegion(string region, int page, int pageSize)
        {
            var result = await _repository.GetToursByRegion(region, page, pageSize);
            var list = _mapper.Map<List<ReadTourDTO>>(result.Items);
            return CreatePagination(list, result.TotalCount, page, pageSize);
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

        public async Task ChangeManagerAsync(int tourId, int newManagerId, int updatedBy)
        {
            var tour = await _repository.GetById(tourId);
            if (tour == null)
            {
                throw new Exception("Tour not found.");
            }

            var gatewayUrl = _configuration.GetValue<string>("GatewayApi:BaseUrl") ?? "https://localhost:7010";
            
            var response = await _httpClient.GetAsync($"{gatewayUrl}/api/users/{newManagerId}");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("User not found or you don't have permission to access this user.");
            }

            var result = await response.Content.ReadFromJsonAsync<UserApiResponse>();
            if (result?.Data == null)
            {
                throw new Exception("Invalid response from Auth API.");
            }

            if (result.Data.Roles == null || !result.Data.Roles.Contains("Manager"))
            {
                throw new Exception("Selected user is not a Manager.");
            }

            tour.CreatedBy = newManagerId;
            tour.UpdatedBy = updatedBy;
            tour.UpdatedAt = DateTime.UtcNow;

            _repository.Update(tour);
            await _repository.SaveChangesAsync();
        }

        public async Task RequestConsultationAsync(ConsultationRequestDto request)
        {
            var tour = await _repository.GetById(request.TourId);
            if (tour == null)
                throw new Exception("Tour not found.");

            var managerId = tour.CreatedBy;
            var serviceKey = _configuration["InternalService:Key"] ?? "StayHubInternalServiceKey_Secret";
            var gatewayUrl = _configuration.GetValue<string>("GatewayApi:BaseUrl") ?? "https://localhost:7010";
            var requestUrl = $"{gatewayUrl.TrimEnd('/')}/api/internal/users/{managerId}";

            var requestMsg = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            requestMsg.Headers.Add("X-Service-Key", serviceKey);

            // Create a custom handler to ignore SSL errors for internal calls
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            using var localClient = new HttpClient(handler);

            var response = await localClient.SendAsync(requestMsg);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to retrieve manager info (ID: {managerId}) via {requestUrl}. Status Code: {response.StatusCode}. Response: {errorBody}");
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            if (!jsonResponse.TryGetProperty("data", out var data) || !data.TryGetProperty("email", out var emailElement))
            {
                throw new Exception($"Manager email not found in response for manager ID {managerId}.");
            }

            var managerEmail = emailElement.GetString();
            if (string.IsNullOrEmpty(managerEmail))
            {
                throw new Exception("Manager email is empty.");
            }

            string subject = $"[StayHub] New Consultation Request for Tour: {tour.Name}";
            string body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>New Consultation Request</title>
</head>
<body style='font-family: ""Helvetica Neue"", Helvetica, Arial, sans-serif; background-color: #f3f4f6; padding: 40px 0; margin: 0; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
        <!-- Header -->
        <div style='background-color: #f97316; padding: 30px 40px; text-align: center;'>
            <h1 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px;'>New Consultation Request</h1>
            <p style='color: #fff8f1; margin: 10px 0 0 0; font-size: 15px; opacity: 0.9;'>A customer is interested in your tour</p>
        </div>
        
        <!-- Content -->
        <div style='padding: 40px;'>
            <div style='background-color: #fff7ed; border-left: 4px solid #f97316; padding: 15px 20px; border-radius: 4px; margin-bottom: 30px;'>
                <p style='margin: 0; font-size: 16px; color: #9a3412;'><strong>Tour:</strong> {tour.Name} <span style='color: #fdba74; font-size: 14px;'>(ID: {tour.Id})</span></p>
            </div>

            <h3 style='color: #1f2937; font-size: 18px; margin: 0 0 20px 0; border-bottom: 1px solid #e5e7eb; padding-bottom: 10px;'>Customer Details</h3>
            
            <table style='width: 100%; border-collapse: collapse; margin-bottom: 30px;'>
                <tr>
                    <td style='padding: 12px 0; border-bottom: 1px solid #f3f4f6; width: 120px; color: #6b7280; font-weight: 500; font-size: 15px;'>Name:</td>
                    <td style='padding: 12px 0; border-bottom: 1px solid #f3f4f6; color: #111827; font-weight: 600; font-size: 15px;'>{request.FullName}</td>
                </tr>
                <tr>
                    <td style='padding: 12px 0; border-bottom: 1px solid #f3f4f6; color: #6b7280; font-weight: 500; font-size: 15px;'>Phone:</td>
                    <td style='padding: 12px 0; border-bottom: 1px solid #f3f4f6; color: #111827; font-weight: 600; font-size: 15px;'><a href='tel:{request.Phone}' style='color: #f97316; text-decoration: none;'>{request.Phone}</a></td>
                </tr>
                <tr>
                    <td style='padding: 12px 0; border-bottom: 1px solid #f3f4f6; color: #6b7280; font-weight: 500; font-size: 15px;'>Email:</td>
                    <td style='padding: 12px 0; border-bottom: 1px solid #f3f4f6; color: #111827; font-weight: 600; font-size: 15px;'><a href='mailto:{request.Email}' style='color: #f97316; text-decoration: none;'>{request.Email}</a></td>
                </tr>
            </table>

            <h3 style='color: #1f2937; font-size: 18px; margin: 0 0 15px 0;'>Customer's Note</h3>
            <div style='background-color: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px; padding: 20px; font-size: 15px; color: #4b5563; line-height: 1.6; white-space: pre-wrap;'>{(string.IsNullOrWhiteSpace(request.Note) ? "<em>No additional notes provided.</em>" : request.Note)}</div>
            
            <div style='margin-top: 40px; text-align: center;'>
                <a href='mailto:{request.Email}' style='display: inline-block; background-color: #f97316; color: #ffffff; text-decoration: none; font-weight: 600; padding: 14px 32px; border-radius: 8px; font-size: 16px;'>Reply to Customer</a>
            </div>
        </div>
        
        <!-- Footer -->
        <div style='background-color: #f9fafb; padding: 24px; text-align: center; border-top: 1px solid #e5e7eb;'>
            <p style='margin: 0; color: #9ca3af; font-size: 13px;'>This is an automated message from StayHub System.</p>
        </div>
    </div>
</body>
</html>";

            await _emailService.SendEmailAsync(managerEmail, subject, body);
        }
    }
}
