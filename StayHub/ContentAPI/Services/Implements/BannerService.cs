using AutoMapper;
using ContentAPI.DTOs;
using ContentAPI.Models;
using ContentAPI.Repositories;

namespace ContentAPI.Services.Implements
{
    public class BannerService : IBannerService
    {
        private readonly IBannerRepository _bannerRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;

        public BannerService(
            IBannerRepository bannerRepository,
            ICloudinaryService cloudinaryService,
            IMapper mapper)
        {
            _bannerRepository = bannerRepository;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
        }

        public async Task<PaginationDTO<ReadBannerDTO>> GetAllBanners(int page, int pageSize)
        {
            var (banners, total) = await _bannerRepository.GetAllPaged(page, pageSize);
            var bannerDtos = _mapper.Map<List<ReadBannerDTO>>(banners);

            return new PaginationDTO<ReadBannerDTO>
            {
                Data = bannerDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        public async Task<PaginationDTO<ReadBannerDTO>> SearchBannersAsync(string keyword, int page, int pageSize)
        {
            var (banners, total) = await _bannerRepository.SearchPagedAsync(keyword, page, pageSize);
            var bannerDtos = _mapper.Map<List<ReadBannerDTO>>(banners);

            return new PaginationDTO<ReadBannerDTO>
            {
                Data = bannerDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<PaginationDTO<ReadBannerDTO>> GetActiveBanners(int page, int pageSize)
        {
            var (banners, total) = await _bannerRepository.GetActiveBannersPaged(page, pageSize);
            var bannerDtos = _mapper.Map<List<ReadBannerDTO>>(banners);

            return new PaginationDTO<ReadBannerDTO>
            {
                Data = bannerDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<ReadBannerDTO?> GetBannerById(int id)
        {
            var banner = await _bannerRepository.GetById(id);
            if (banner == null) return null;

            return _mapper.Map<ReadBannerDTO>(banner);
        }

        public async Task<ReadBannerDTO> CreateBanner(CreateBannerDTO dto)
        {
            string imageUrl = string.Empty;

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.ImageFile, "StayHub_Banners");
                if (uploadResult.Error == null)
                {
                    imageUrl = uploadResult.SecureUrl.ToString();
                }
            }

            var newBanner = new Banner
            {
                Title = dto.Title,
                TargetUrl = dto.TargetUrl,
                Priority = dto.Priority ?? 0,
                IsActive = dto.IsActive ?? true,
                ImageUrl = imageUrl
            };

            await _bannerRepository.Add(newBanner);
            return _mapper.Map<ReadBannerDTO>(newBanner);
        }

        public async Task<bool> UpdateBanner(int id, UpdateBannerDTO dto)
        {
            var existingBanner = await _bannerRepository.GetById(id);
            if (existingBanner == null) return false;

            if (dto.ImageFile != null && dto.ImageFile.Length > 0)
            {
                string? oldPublicId = _cloudinaryService.ExtractPublicIdFromUrl(existingBanner.ImageUrl);

                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    await _cloudinaryService.DeleteImageAsync(oldPublicId);
                }

                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.ImageFile, "StayHub_Banners");
                if (uploadResult.Error == null)
                {
                    existingBanner.ImageUrl = uploadResult.SecureUrl.ToString();
                }
            }

            existingBanner.Title = dto.Title;
            existingBanner.TargetUrl = dto.TargetUrl;
            if (dto.Priority.HasValue) existingBanner.Priority = dto.Priority.Value;
            if (dto.IsActive.HasValue) existingBanner.IsActive = dto.IsActive.Value;

            await _bannerRepository.Update(id, existingBanner);
            return true;
        }

        public async Task<bool> DeleteBanner(int id)
        {
            var existingBanner = await _bannerRepository.GetById(id);
            if (existingBanner == null) return false;

            string? publicIdToDelete = _cloudinaryService.ExtractPublicIdFromUrl(existingBanner.ImageUrl);

            if (!string.IsNullOrEmpty(publicIdToDelete))
            {
                await _cloudinaryService.DeleteImageAsync(publicIdToDelete);
            }

            await _bannerRepository.Delete(id);
            return true;
        }

        public async Task<bool> ChangeBannerStatus(int id, bool isActive)
        {
            var existingBanner = await _bannerRepository.GetById(id);
            if (existingBanner == null) return false;

            existingBanner.IsActive = isActive;

            await _bannerRepository.Update(id, existingBanner);
            return true;
        }
    }
}