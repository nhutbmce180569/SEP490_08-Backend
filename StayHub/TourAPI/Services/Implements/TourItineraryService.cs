using AutoMapper;
using System.Linq;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class TourItineraryService : ITourItineraryService
    {
        private readonly ITourItineraryRepository _repository;
        private readonly ITourRepository _tourRepository;
        private readonly IMapper _mapper;

        public TourItineraryService(ITourItineraryRepository repository, ITourRepository tourRepository, IMapper mapper)
        {
            _repository = repository;
            _tourRepository = tourRepository;
            _mapper = mapper;
        }

        public async Task<PaginationDTO<ReadTourItineraryDTO>> GetAll(int page, int pageSize)
        {
            var entities = await _repository.GetAllAsync();
            var list = _mapper.Map<List<ReadTourItineraryDTO>>(entities);
            int total = list.Count;

            list = list
                    .OrderBy(x => x.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

            return new PaginationDTO<ReadTourItineraryDTO>
            {
                Data = list,
                Total = total,
                TotalPages = (int)Math.Ceiling((double)total / pageSize),
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        public async Task<ReadTourItineraryDTO?> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity == null ? null : _mapper.Map<ReadTourItineraryDTO>(entity);
        }

        public async Task Add(CreateTourItineraryDTO dto, int operatorId)
        {
            ValidateTimeRange(dto.StartDuration, dto.EndDuration);

            var tour = await _tourRepository.GetById(dto.TourId);
            if (tour == null) throw new Exception($"Tour with Id {dto.TourId} not found");

            await ValidateStartDurationUniqueness(dto.TourId, dto.DayNumber, dto.StartDuration);

            var entity = _mapper.Map<TourItinerary>(dto);
            await _repository.AddAsync(entity);
        }

        public async Task Update(int id, UpdateTourItineraryDTO dto, int operatorId)
        {
            ValidateTimeRange(dto.StartDuration, dto.EndDuration);

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new Exception("TourItinerary not found");

            if (dto.TourId != entity.TourId) throw new Exception("Cannot change TourId of an itinerary");

            var tour = await _tourRepository.GetById(entity.TourId);
            if (tour == null) throw new Exception($"Tour with Id {entity.TourId} not found");

            await ValidateStartDurationUniqueness(entity.TourId, dto.DayNumber, dto.StartDuration, id);

            _mapper.Map(dto, entity);
            await _repository.UpdateAsync(entity);
        }

        public async Task Delete(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new Exception("TourItinerary not found");

            await _repository.DeleteAsync(entity);
        }

        public async Task AddBatch(CreateTourItineraryBatchDTO batch, int operatorId)
        {
            if (batch?.Itineraries == null || !batch.Itineraries.Any())
                throw new Exception("Itineraries list cannot be empty");

            var tourId = batch.Itineraries.First().TourId;

            if (!batch.Itineraries.All(x => x.TourId == tourId))
                throw new Exception("All itineraries must have the same TourId");

            var tourExists = await _tourRepository.GetById(tourId);

            if (tourExists == null)
                throw new Exception($"Tour with Id {tourId} not found");


            foreach (var itinerary in batch.Itineraries)
            {
                ValidateTimeRange(itinerary.StartDuration, itinerary.EndDuration);
            }

            var duplicatedStartDurationsInRequest = batch.Itineraries
                .GroupBy(x => new { x.DayNumber, x.StartDuration })
                .Where(g => g.Count() > 1)
                .Select(g => $"day {g.Key.DayNumber} at {g.Key.StartDuration:HH\\:mm}")
                .ToList();

            if (duplicatedStartDurationsInRequest.Any())
            {
                throw new Exception(
                    $"Batch contains duplicated Start Duration in same day: {string.Join(", ", duplicatedStartDurationsInRequest)}");
            }

            foreach (var itinerary in batch.Itineraries)
            {
                await ValidateStartDurationUniqueness(itinerary.TourId, itinerary.DayNumber, itinerary.StartDuration);
            }

            var itineraries = _mapper.Map<List<TourItinerary>>(batch.Itineraries);
            await _repository.AddRange(itineraries);
        }

        private static void ValidateTimeRange(TimeOnly? startDuration, TimeOnly? endDuration)
        {
            if (!startDuration.HasValue || !endDuration.HasValue)
            {
                throw new Exception("Start Duration and End Duration are required.");
            }

            if (endDuration.Value <= startDuration.Value)
            {
                throw new Exception("End Duration must be later than Start Duration.");
            }
        }

        private async Task ValidateStartDurationUniqueness(
            int tourId,
            int dayNumber,
            TimeOnly? startDuration,
            int? exceptId = null)
        {
            if (!startDuration.HasValue)
            {
                throw new Exception("StartDuration is required.");
            }

            var duplicatedStart = await _repository.ExistsByTourDayAndStartDuration(
                tourId,
                dayNumber,
                startDuration.Value,
                exceptId);

            if (duplicatedStart)
            {
                throw new Exception(
                    $"Start Duration {startDuration:HH\\:mm} already exists for day {dayNumber}.");
            }
        }

    }
}
