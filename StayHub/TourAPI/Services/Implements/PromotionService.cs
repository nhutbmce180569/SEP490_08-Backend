using AutoMapper;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepository;
        private readonly IMapper _mapper;

        public PromotionService(IPromotionRepository promotionRepository, IMapper mapper)
        {
            _promotionRepository = promotionRepository;
            _mapper = mapper;
        }

        public async Task<ReadPromotionDTO> CreatePromotionAsync(CreatePromotionDTO dto)
        {
            if (dto.StartDate >= dto.EndDate)
            {
                throw new ArgumentException("StartDate must be before EndDate");
            }

            if (dto.DiscountType == "PERCENTAGE" && (dto.DiscountValue < 1 || dto.DiscountValue > 100))
            {
                throw new ArgumentException("Percentage discount must be between 1 and 100");
            }

            if (dto.DiscountType == "FIXED" && dto.DiscountValue < 10000)
            {
                throw new ArgumentException("Fixed discount must be at least 10,000 VND");
            }

            if (dto.DiscountType == "PERCENTAGE" && dto.MaxDiscountAmount.HasValue && dto.MaxDiscountAmount.Value < 10000)
            {
                throw new ArgumentException("Max discount amount must be at least 10,000 VND");
            }

            var existingPromotion = await _promotionRepository.GetByCode(dto.Code);
            if (existingPromotion != null)
            {
                throw new ArgumentException($"Promotion with code {dto.Code} already exists");
            }

            var promotion = _mapper.Map<Promotion>(dto);
            promotion.CreatedAt = DateTime.UtcNow;
            
            if (string.IsNullOrEmpty(promotion.Status))
            {
                promotion.Status = "Active";
            }

            await _promotionRepository.Add(promotion);
            await _promotionRepository.SaveChangesAsync();

            return _mapper.Map<ReadPromotionDTO>(promotion);
        }

        public async Task<ReadPromotionDTO> ChangePromotionStatusAsync(int id, string status)
        {
            var promotion = await _promotionRepository.GetById(id);
            if (promotion == null)
            {
                throw new KeyNotFoundException($"Promotion with ID {id} not found");
            }

            promotion.Status = status;
            _promotionRepository.Update(promotion);
            await _promotionRepository.SaveChangesAsync();

            return _mapper.Map<ReadPromotionDTO>(promotion);
        }

        public async Task<(List<ReadPromotionDTO> Promotions, int Total)> GetAllPromotionsAsync(int page, int pageSize, string? searchTerm = null, string? status = null)
        {
            var result = await _promotionRepository.GetAll(page, pageSize, searchTerm, status);
            var dtos = _mapper.Map<List<ReadPromotionDTO>>(result.Promotions);
            return (dtos, result.Total);
        }

        public async Task<ReadPromotionDTO> GetPromotionByIdAsync(int id)
        {
            var promotion = await _promotionRepository.GetById(id);
            if (promotion == null)
            {
                throw new KeyNotFoundException($"Promotion with ID {id} not found");
            }

            return _mapper.Map<ReadPromotionDTO>(promotion);
        }

        public async Task<ReadPromotionDTO> UpdatePromotionAsync(int id, UpdatePromotionDTO dto)
        {
            if (dto.StartDate >= dto.EndDate)
            {
                throw new ArgumentException("StartDate must be before EndDate");
            }

            if (dto.DiscountType == "PERCENTAGE" && (dto.DiscountValue < 1 || dto.DiscountValue > 100))
            {
                throw new ArgumentException("Percentage discount must be between 1 and 100");
            }

            if (dto.DiscountType == "FIXED" && dto.DiscountValue < 10000)
            {
                throw new ArgumentException("Fixed discount must be at least 10,000 VND");
            }

            if (dto.DiscountType == "PERCENTAGE" && dto.MaxDiscountAmount.HasValue && dto.MaxDiscountAmount.Value < 10000)
            {
                throw new ArgumentException("Max discount amount must be at least 10,000 VND");
            }

            var promotion = await _promotionRepository.GetById(id);
            if (promotion == null)
            {
                throw new KeyNotFoundException($"Promotion with ID {id} not found");
            }

            if (promotion.Code != dto.Code)
            {
                throw new ArgumentException("Cannot change Promotion Code");
            }

            if (promotion.DiscountType != dto.DiscountType)
            {
                throw new ArgumentException("Cannot change Discount Type");
            }

            _mapper.Map(dto, promotion);

            if (string.IsNullOrEmpty(promotion.Status))
            {
                promotion.Status = "Active";
            }

            _promotionRepository.Update(promotion);
            await _promotionRepository.SaveChangesAsync();

            return _mapper.Map<ReadPromotionDTO>(promotion);
        }
    }
}
