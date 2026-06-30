using AutoMapper;
using ContentAPI.Constants;
using ContentAPI.DTOs;
using ContentAPI.Models;
using ContentAPI.Repositories;

namespace ContentAPI.Services.Implements
{
    public class TourismInformationService : ITourismInformationService
    {
        private readonly ITourismInformationRepository _repository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;

        public TourismInformationService(
            ITourismInformationRepository repository,
            ICloudinaryService cloudinaryService,
            IMapper mapper)
        {
            _repository = repository;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
        }

        public async Task<PaginationDTO<ReadTourismInformationDTO>> GetAllAsync(
            int page,
            int pageSize,
            string? searchTerm,
            string? type,
            string? status,
            string? city)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            ValidateOptionalFilters(type, status);

            var (items, total) = await _repository.GetAllPagedAsync(
                page, pageSize, searchTerm, type, status, city);

            return BuildPagination(items, total, page, pageSize);
        }

        public async Task<PaginationDTO<ReadTourismInformationDTO>> GetActiveAsync(int page, int pageSize)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var (items, total) = await _repository.GetActivePagedAsync(page, pageSize);
            return BuildPagination(items, total, page, pageSize);
        }

        public async Task<ReadTourismInformationDTO?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ReadTourismInformationDTO>(entity);
        }

        public async Task<ReadTourismInformationDTO> CreateAsync(CreateTourismInformationDTO dto)
        {
            ValidateType(dto.Type);
            var (parsedLat, parsedLng) = ParseCoordinates(dto.Latitude, dto.Longitude);
            ValidateCoordinates(parsedLat, parsedLng);
            ValidateSourceUrl(dto.SourceUrl);

            var imageUrl = await UploadImageAsync(dto.ImageFile);

            var entity = new TourismInformation
            {
                Name = dto.Name.Trim(),
                Type = dto.Type.Trim(),
                Description = dto.Description?.Trim(),
                Address = dto.Address?.Trim(),
                City = dto.City?.Trim(),
                Country = string.IsNullOrWhiteSpace(dto.Country)
                    ? TourismInformationConstants.DefaultCountry
                    : dto.Country.Trim(),
                Latitude = parsedLat,
                Longitude = parsedLng,
                ImageUrl = imageUrl,
                SourceName = dto.SourceName?.Trim(),
                SourceUrl = dto.SourceUrl?.Trim(),
                Status = TourismInformationConstants.StatusActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _repository.AddAsync(entity);
            return _mapper.Map<ReadTourismInformationDTO>(entity);
        }

        public async Task<bool> UpdateAsync(int id, UpdateTourismInformationDTO dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            ValidateType(dto.Type);
            var (parsedLat, parsedLng) = ParseCoordinates(dto.Latitude, dto.Longitude);
            ValidateCoordinates(parsedLat, parsedLng);
            ValidateSourceUrl(dto.SourceUrl);

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                await DeleteCloudinaryImageIfExists(entity.ImageUrl);
                entity.ImageUrl = await UploadImageAsync(dto.ImageFile);
            }

            entity.Name = dto.Name.Trim();
            entity.Type = dto.Type.Trim();
            entity.Description = dto.Description?.Trim();
            entity.Address = dto.Address?.Trim();
            entity.City = dto.City?.Trim();
            entity.Country = string.IsNullOrWhiteSpace(dto.Country)
                ? TourismInformationConstants.DefaultCountry
                : dto.Country.Trim();
            entity.Latitude = parsedLat;
            entity.Longitude = parsedLng;
            entity.SourceName = dto.SourceName?.Trim();
            entity.SourceUrl = dto.SourceUrl?.Trim();
            entity.UpdatedAt = DateTime.Now;

            await _repository.UpdateAsync(entity);
            return true;
        }

        public async Task<bool> ChangeStatusAsync(int id, bool isActive)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            var targetStatus = isActive
                ? TourismInformationConstants.StatusActive
                : TourismInformationConstants.StatusInactive;

            if (entity.Status == targetStatus)
            {
                throw new InvalidOperationException(
                    isActive
                        ? "Tourism information is already active."
                        : "Tourism information is already inactive.");
            }

            entity.Status = targetStatus;
            entity.UpdatedAt = DateTime.Now;

            await _repository.UpdateAsync(entity);
            return true;
        }

        private async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            var uploadResult = await _cloudinaryService.UploadImageAsync(
                imageFile, TourismInformationConstants.CloudinaryFolder);

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException(
                    "Error uploading image to Cloudinary: " + uploadResult.Error.Message);
            }

            if (uploadResult.SecureUrl == null)
            {
                throw new InvalidOperationException("Failed to upload image to Cloudinary.");
            }

            return uploadResult.SecureUrl.ToString();
        }

        private async Task DeleteCloudinaryImageIfExists(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            var publicId = _cloudinaryService.ExtractPublicIdFromUrl(imageUrl);
            if (publicId != null)
            {
                await _cloudinaryService.DeleteImageAsync(publicId);
            }
        }

        private PaginationDTO<ReadTourismInformationDTO> BuildPagination(
            List<TourismInformation> items,
            int total,
            int page,
            int pageSize)
        {
            return new PaginationDTO<ReadTourismInformationDTO>
            {
                Data = _mapper.Map<List<ReadTourismInformationDTO>>(items),
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        private static void ValidateType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new InvalidOperationException("Type is required.");
            }

            if (!TourismInformationConstants.ValidTypes.Contains(type.Trim()))
            {
                throw new InvalidOperationException(
                    $"Invalid type. Allowed values: {string.Join(", ", TourismInformationConstants.ValidTypes)}.");
            }
        }

        private static void ValidateCoordinates(double? latitude, double? longitude)
        {
            if (latitude.HasValue != longitude.HasValue)
            {
                throw new InvalidOperationException("Latitude and longitude must both be provided or both be omitted.");
            }

            if (latitude.HasValue && (latitude.Value < -90 || latitude.Value > 90))
            {
                throw new InvalidOperationException("Latitude must be between -90 and 90.");
            }

            if (longitude.HasValue && (longitude.Value < -180 || longitude.Value > 180))
            {
                throw new InvalidOperationException("Longitude must be between -180 and 180.");
            }
        }

        private static (double? Latitude, double? Longitude) ParseCoordinates(string? latStr, string? lngStr)
        {
            double? parsedLat = !string.IsNullOrWhiteSpace(latStr)
                && double.TryParse(latStr.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat)
                ? lat : null;

            double? parsedLng = !string.IsNullOrWhiteSpace(lngStr)
                && double.TryParse(lngStr.Replace(",", "."), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lng)
                ? lng : null;

            return (parsedLat, parsedLng);
        }

        private static void ValidateSourceUrl(string? sourceUrl)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                return;
            }

            if (!Uri.TryCreate(sourceUrl.Trim(), UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException("Source URL must be a valid HTTP or HTTPS URL.");
            }
        }

        private static void ValidateOptionalFilters(string? type, string? status)
        {
            if (!string.IsNullOrWhiteSpace(type) && !TourismInformationConstants.ValidTypes.Contains(type.Trim()))
            {
                throw new InvalidOperationException(
                    $"Invalid type filter. Allowed values: {string.Join(", ", TourismInformationConstants.ValidTypes)}.");
            }

            if (!string.IsNullOrWhiteSpace(status)
                && status.Trim() != TourismInformationConstants.StatusActive
                && status.Trim() != TourismInformationConstants.StatusInactive)
            {
                throw new InvalidOperationException(
                    $"Invalid status filter. Allowed values: {TourismInformationConstants.StatusActive}, {TourismInformationConstants.StatusInactive}.");
            }
        }
    }
}
