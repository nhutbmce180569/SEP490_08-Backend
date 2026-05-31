using AutoMapper;
using ContentAPI.DTOs;
using ContentAPI.Helpers;
using ContentAPI.Models;
using ContentAPI.Repositories;
using System.Net.Http;

namespace ContentAPI.Services.Implements
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IMapper _mapper;
        private readonly HttpClient _httpClient; 
        private readonly IConfiguration _configuration;
        private readonly ITourApiClient _tourApiClient;

        public CategoryService(
            ICategoryRepository categoryRepository,
            ICloudinaryService cloudinaryService,
            IMapper mapper,
            HttpClient httpClient,
            IConfiguration configuration, 
            ITourApiClient tourApiClient)
        {
            _categoryRepository = categoryRepository;
            _cloudinaryService = cloudinaryService;
            _mapper = mapper;
            _httpClient = httpClient;
            _configuration = configuration;
            _tourApiClient = tourApiClient;
        }

        public async Task<PaginationDTO<ReadCategoryDTO>> GetAllCategories(int page, int pageSize)
        {
            var (categories, total) = await _categoryRepository.GetAllPaged(page, pageSize);
            var categoryDtos = _mapper.Map<List<ReadCategoryDTO>>(categories);

            return new PaginationDTO<ReadCategoryDTO>
            {
                Data = categoryDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        public async Task<PaginationDTO<ReadCategoryDTO>> SearchCategoriesAsync(string keyword, int page, int pageSize)
        {
            var (categories, total) = await _categoryRepository.SearchPagedAsync(keyword, page, pageSize);
            var categoryDtos = _mapper.Map<List<ReadCategoryDTO>>(categories);

            return new PaginationDTO<ReadCategoryDTO>
            {
                Data = categoryDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }
        public async Task<PaginationDTO<ReadCategoryDTO>> GetActiveCategories(int page, int pageSize)
        {
            var (categories, total) = await _categoryRepository.GetActiveCategoriesPaged(page, pageSize);
            var categoryDtos = _mapper.Map<List<ReadCategoryDTO>>(categories);

            return new PaginationDTO<ReadCategoryDTO>
            {
                Data = categoryDtos,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<ReadCategoryDTO?> GetCategoryById(int id)
        {
            var category = await _categoryRepository.GetById(id);
            return _mapper.Map<ReadCategoryDTO>(category);
        }

        public async Task<ReadCategoryDTO> CreateCategory(CreateCategoryDTO dto)
        {
            string iconUrl = string.Empty;

            if (dto.IconFile != null && dto.IconFile.Length > 0)
            {
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.IconFile, "StayHub_Categories");
                if (uploadResult.Error == null)
                {
                    iconUrl = uploadResult.SecureUrl.ToString();
                }
            }

            var category = new Category
            {
                Name = dto.Name,
                Slug = dto.Slug,
                Description = dto.Description,
                IsActive = dto.IsActive ?? true,
                IconUrl = iconUrl
            };

            await _categoryRepository.Add(category);
            return _mapper.Map<ReadCategoryDTO>(category);
        }

        public async Task<bool> UpdateCategory(int id, UpdateCategoryDTO dto)
        {
            var existingCategory = await _categoryRepository.GetById(id);
            if (existingCategory == null) return false;

            if (dto.IconFile != null && dto.IconFile.Length > 0)
            {
                // Xóa icon cũ trên Cloudinary
                string? oldPublicId = _cloudinaryService.ExtractPublicIdFromUrl(existingCategory.IconUrl);
                if (!string.IsNullOrEmpty(oldPublicId))
                {
                    await _cloudinaryService.DeleteImageAsync(oldPublicId);
                }

                // Upload icon mới
                var uploadResult = await _cloudinaryService.UploadImageAsync(dto.IconFile, "StayHub_Categories");
                if (uploadResult.Error == null)
                {
                    existingCategory.IconUrl = uploadResult.SecureUrl.ToString();
                }
            }

            existingCategory.Name = dto.Name;
            existingCategory.Slug = dto.Slug;
            existingCategory.Description = dto.Description;
            if (dto.IsActive.HasValue) existingCategory.IsActive = dto.IsActive.Value;

            await _categoryRepository.Update(id, existingCategory);
            return true;
        }

        public async Task<bool> DeleteCategory(int id)
        {
            var category = await _categoryRepository.GetById(id);
            if (category == null) return false;

            try
            {
                var tourCount = await _tourApiClient.GetTourCountByCategoryIdAsync(id);
                if (tourCount > 0)
                {
                    throw new InvalidOperationException("Cannot delete this category because there are tours associated with it.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Cannot verify dependencies with TourAPI. Deletion aborted.", ex);
            }

            string? publicId = _cloudinaryService.ExtractPublicIdFromUrl(category.IconUrl);
            if (!string.IsNullOrEmpty(publicId))
            {
                await _cloudinaryService.DeleteImageAsync(publicId);
            }

            await _categoryRepository.Delete(id);
            return true;
        }

        public async Task<bool> ChangeCategoryStatus(int id, bool isActive)
        {
            var existingCategory = await _categoryRepository.GetById(id);
            if (existingCategory == null) return false;

            existingCategory.IsActive = isActive;

            await _categoryRepository.Update(id, existingCategory);
            return true;
        }
    }
}