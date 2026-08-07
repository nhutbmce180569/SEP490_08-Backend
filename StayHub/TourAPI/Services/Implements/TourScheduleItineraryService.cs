using AutoMapper;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TourAPI.DTOs;
using TourAPI.Models;
using TourAPI.Repositories;
using TourAPI.Services;

namespace TourAPI.Services.Implements
{
    public class TourScheduleItineraryService : ITourScheduleItineraryService
    {
        private readonly ITourScheduleItineraryRepository _repository;
        private readonly ITourScheduleRepository _tourScheduleRepository;
        private readonly ITourRepository _tourRepository;
        private readonly IMapper _mapper;
        private readonly IBookingApiClient _bookingApiClient;

        public TourScheduleItineraryService(
            ITourScheduleItineraryRepository repository,
            ITourScheduleRepository tourScheduleRepository,
            ITourRepository tourRepository,
            IMapper mapper,
            IBookingApiClient bookingApiClient)
        {
            _repository = repository;
            _tourScheduleRepository = tourScheduleRepository;
            _tourRepository = tourRepository;
            _mapper = mapper;
            _bookingApiClient = bookingApiClient;
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

            if (await _bookingApiClient.HasOrdersForScheduleAsync(dto.ScheduleId))
            {
                throw new Exception("Cannot add new itinerary items to a schedule that has paid orders.");
            }

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

            if (await _bookingApiClient.HasOrdersForScheduleAsync(entity.ScheduleId))
            {
                // BR-36: Block core content updates by preserving existing values.
                // Only time adjustments (StartDuration, EndDuration) are permitted.
                dto.Title = entity.Title;
                dto.Description = entity.Description;
                dto.LocationName = entity.LocationName;
                dto.LocationLat = entity.LocationLat;
                dto.LocationLng = entity.LocationLng;
                dto.TourismInfoId = entity.TourismInfoId;
                dto.DayNumber = entity.DayNumber;
                dto.ItineraryDate = entity.ItineraryDate;
            }

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

            if (await _bookingApiClient.HasOrdersForScheduleAsync(entity.ScheduleId))
            {
                throw new Exception("Cannot delete itinerary items from a schedule that has paid orders.");
            }

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

            if (await _bookingApiClient.HasOrdersForScheduleAsync(scheduleId))
            {
                throw new Exception("Cannot add new itinerary items to a schedule that has paid orders.");
            }

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

        public byte[] CreateImportTemplate()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("ScheduleItineraries");
            var headers = new[]
            {
                "DayNumber", "ItineraryDate", "Title", "Description", "StartTime",
                "EndTime", "LocationName", "Latitude", "Longitude", "TourismName"
            };

            for (var column = 1; column <= headers.Length; column++)
            {
                sheet.Cell(1, column).Value = headers[column - 1];
            }

            sheet.Range(1, 1, 1, headers.Length).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#E0E7FF"));

            sheet.Cell(2, 1).Value = 1;
            sheet.Cell(2, 2).Value = "2026-01-01";
            sheet.Cell(2, 3).Value = "Morning city tour";
            sheet.Cell(2, 4).Value = "Visit the city center and learn about local history.";
            sheet.Cell(2, 5).Value = "08:00";
            sheet.Cell(2, 6).Value = "10:30";
            sheet.Cell(2, 7).Value = "Ben Thanh Market";
            sheet.Cell(2, 8).Value = "";
            sheet.Cell(2, 9).Value = "";
            sheet.Cell(2, 10).Value = "";
            sheet.SheetView.FreezeRows(1);
            sheet.Columns().AdjustToContents();

            var instructions = workbook.Worksheets.Add("Instructions");
            instructions.Cell(1, 1).Value = "Column";
            instructions.Cell(1, 2).Value = "Required";
            instructions.Cell(1, 3).Value = "Notes";
            instructions.Range(1, 1, 1, 3).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#E0E7FF"));

            var instructionRows = new[]
            {
                new[] { "DayNumber", "Yes", "Positive whole number. Rows on the same day must use the same date." },
                new[] { "ItineraryDate", "Yes", "Use YYYY-MM-DD format and select a date appropriate for the schedule." },
                new[] { "Title", "Yes", "Between 3 and 255 characters." },
                new[] { "Description", "No", "May be left blank; maximum 2000 characters." },
                new[] { "StartTime", "Yes", "Use HH:mm format, for example 08:00." },
                new[] { "EndTime", "Yes", "Use HH:mm format and enter a time later than StartTime." },
                new[] { "LocationName", "No", "May be left blank; maximum 255 characters." },
                new[] { "Latitude", "No", "May be left blank. Pick the precise location on the web before saving." },
                new[] { "Longitude", "No", "May be left blank. Pick the precise location on the web before saving." },
                new[] { "TourismName", "No", "Enter a tourism place name to prefill the search box, or leave blank." },
            };

            for (var row = 0; row < instructionRows.Length; row++)
            {
                for (var column = 0; column < instructionRows[row].Length; column++)
                {
                    instructions.Cell(row + 2, column + 1).Value = instructionRows[row][column];
                }
            }

            instructions.SheetView.FreezeRows(1);
            instructions.Columns().AdjustToContents();
            instructions.Column(3).Width = Math.Min(instructions.Column(3).Width, 70);
            instructions.Column(3).Style.Alignment.WrapText = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
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
