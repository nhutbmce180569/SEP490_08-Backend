using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;

namespace TourAPI.Services.Implements
{
    public class TourScheduleItineraryService : ITourScheduleItineraryService
    {
        private readonly ITourScheduleItineraryRepository _repository;
        private readonly ITourScheduleRepository _tourScheduleRepository;
        private readonly ITourRepository _tourRepository;
        private readonly IMapper _mapper;

        public TourScheduleItineraryService(
            ITourScheduleItineraryRepository repository,
            ITourScheduleRepository tourScheduleRepository,
            ITourRepository tourRepository,
            IMapper mapper)
        {
            _repository = repository;
            _tourScheduleRepository = tourScheduleRepository;
            _tourRepository = tourRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<ReadTourScheduleItineraryDTO>> GetByScheduleId(int scheduleId)
        {
            var entities = await _repository.GetByScheduleIdAsync(scheduleId);
            return _mapper.Map<List<ReadTourScheduleItineraryDTO>>(entities);
        }

        public async Task<ReadTourScheduleItineraryDTO?> GetById(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return null;
            return _mapper.Map<ReadTourScheduleItineraryDTO>(entity);
        }

        public async Task<ReadTourScheduleItineraryDTO> Add(CreateTourScheduleItineraryDTO dto, int operatorId)
        {
            ValidateTimeRange(dto.StartDuration, dto.EndDuration);

            var schedule = await _tourScheduleRepository.GetByIdAsync(dto.ScheduleId);
            if (schedule == null) throw new Exception($"Tour schedule with Id {dto.ScheduleId} not found");

            var tour = await _tourRepository.GetById(schedule.TourId);
            if (tour == null) throw new Exception($"Tour with Id {schedule.TourId} not found");

            await ValidateDayDateMapping(dto.ScheduleId, dto.DayNumber, dto.ItineraryDate);
            await ValidateStartDurationUniqueness(dto.ScheduleId, dto.ItineraryDate, dto.StartDuration);

            var entity = _mapper.Map<TourScheduleItinerary>(dto);
            await _repository.AddAsync(entity);
            return _mapper.Map<ReadTourScheduleItineraryDTO>(entity);
        }

        public async Task Update(int id, UpdateTourScheduleItineraryDTO dto, int operatorId)
        {
            ValidateTimeRange(dto.StartDuration, dto.EndDuration);

            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new Exception("TourScheduleItinerary not found");

            if (dto.ScheduleId != entity.ScheduleId) throw new Exception("Cannot change ScheduleId of an itinerary");

            var schedule = await _tourScheduleRepository.GetByIdAsync(entity.ScheduleId);
            if (schedule == null) throw new Exception($"Tour schedule with Id {entity.ScheduleId} not found");

            var tour = await _tourRepository.GetById(schedule.TourId);
            if (tour == null) throw new Exception($"Tour with Id {schedule.TourId} not found");

            await ValidateDayDateMapping(entity.ScheduleId, dto.DayNumber, dto.ItineraryDate, id);
            await ValidateStartDurationUniqueness(entity.ScheduleId, dto.ItineraryDate, dto.StartDuration, id);

            _mapper.Map(dto, entity);
            await _repository.UpdateAsync(entity);
        }

        public async Task Delete(int id, int operatorId)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) throw new Exception("TourScheduleItinerary not found");

            var schedule = await _tourScheduleRepository.GetByIdAsync(entity.ScheduleId);
            if (schedule == null) throw new Exception($"Tour schedule with Id {entity.ScheduleId} not found");

            var tour = await _tourRepository.GetById(schedule.TourId);
            if (tour == null) throw new Exception($"Tour with Id {schedule.TourId} not found");

            await _repository.DeleteAsync(entity);
        }

        public async Task AddBatch(CreateTourScheduleItineraryBatchDTO batch, int operatorId)
        {
            if (batch?.Itineraries == null || !batch.Itineraries.Any())
                throw new Exception("Itineraries list cannot be empty");

            var scheduleId = batch.Itineraries.First().ScheduleId;

            if (!batch.Itineraries.All(x => x.ScheduleId == scheduleId))
                throw new Exception("All itineraries must have the same ScheduleId");

            var scheduleExists = await _tourScheduleRepository.GetByIdAsync(scheduleId);
            if (scheduleExists == null)
                throw new Exception($"Tour schedule with Id {scheduleId} not found");

            var tour = await _tourRepository.GetById(scheduleExists.TourId);
            if (tour == null) throw new Exception($"Tour with Id {scheduleExists.TourId} not found");

            foreach (var itinerary in batch.Itineraries)
            {
                ValidateTimeRange(itinerary.StartDuration, itinerary.EndDuration);
            }

            var duplicateDayDateInRequest = batch.Itineraries
                .GroupBy(x => x.ItineraryDate.Date)
                .Where(g => g.Select(x => x.DayNumber).Distinct().Count() > 1)
                .Select(g => $"{g.Key:yyyy-MM-dd}")
                .ToList();

            if (duplicateDayDateInRequest.Any())
            {
                throw new Exception(
                    $"Batch contains conflicting DayNumber for dates: {string.Join(", ", duplicateDayDateInRequest)}");
            }

            var duplicateDateByDayInRequest = batch.Itineraries
                .GroupBy(x => x.DayNumber)
                .Where(g => g.Select(x => x.ItineraryDate.Date).Distinct().Count() > 1)
                .Select(g => g.Key.ToString())
                .ToList();

            if (duplicateDateByDayInRequest.Any())
            {
                throw new Exception(
                    $"Batch contains conflicting ItineraryDate for DayNumber: {string.Join(", ", duplicateDateByDayInRequest)}");
            }

            var duplicatedStartDurationsInRequest = batch.Itineraries
                .GroupBy(x => new { Date = x.ItineraryDate.Date, x.StartDuration })
                .Where(g => g.Count() > 1)
                .Select(g => $"date {g.Key.Date:yyyy-MM-dd} at {g.Key.StartDuration:HH\\:mm}")
                .ToList();

            if (duplicatedStartDurationsInRequest.Any())
            {
                throw new Exception(
                    $"Batch contains duplicated Start Duration in same date: {string.Join(", ", duplicatedStartDurationsInRequest)}");
            }

            foreach (var itinerary in batch.Itineraries)
            {
                await ValidateDayDateMapping(
                    itinerary.ScheduleId,
                    itinerary.DayNumber,
                    itinerary.ItineraryDate);

                await ValidateStartDurationUniqueness(
                    itinerary.ScheduleId,
                    itinerary.ItineraryDate,
                    itinerary.StartDuration);
            }

            var itineraries = _mapper.Map<List<TourScheduleItinerary>>(batch.Itineraries);
            await _repository.AddRangeAsync(itineraries);
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
            int scheduleId,
            DateTime itineraryDate,
            TimeOnly? startDuration,
            int? exceptId = null)
        {
            if (!startDuration.HasValue)
            {
                throw new Exception("StartDuration is required.");
            }

            var duplicatedStart = await _repository.ExistsByScheduleDateAndStartDuration(
                scheduleId,
                itineraryDate,
                startDuration.Value,
                exceptId);

            if (duplicatedStart)
            {
                throw new Exception(
                    $"Start Duration {startDuration:HH\\:mm} already exists for date {itineraryDate:yyyy-MM-dd}.");
            }
        }

        private async Task ValidateDayDateMapping(
            int scheduleId,
            int dayNumber,
            DateTime itineraryDate,
            int? exceptId = null)
        {
            var hasConflict = await _repository.HasDayDateConflict(
                scheduleId,
                dayNumber,
                itineraryDate,
                exceptId);

            if (hasConflict)
            {
                throw new Exception(
                    $"DayNumber {dayNumber} and date {itineraryDate:yyyy-MM-dd} are inconsistent with existing schedule itinerary mapping.");
            }
        }
    }
}
