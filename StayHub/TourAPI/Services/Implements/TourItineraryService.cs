using AutoMapper;
using ClosedXML.Excel;
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
            NormalizeAndValidate(dto);
            ValidateTimeRange(dto.StartDuration, dto.EndDuration);

            var tour = await _tourRepository.GetById(dto.TourId);
            if (tour == null) throw new Exception($"Tour with Id {dto.TourId} not found");

            await ValidateStartDurationUniqueness(dto.TourId, dto.DayNumber, dto.StartDuration);

            var entity = _mapper.Map<TourItinerary>(dto);
            await _repository.AddAsync(entity);
        }

        public async Task Update(int id, UpdateTourItineraryDTO dto, int operatorId)
        {
            NormalizeAndValidate(dto);
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
                NormalizeAndValidate(itinerary);
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

        public byte[] CreateImportTemplate()
        {
            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Itineraries");
            var headers = new[]
            {
                "DayNumber", "Title", "Description", "StartTime", "EndTime",
                "LocationName", "Latitude", "Longitude", "TourismName"
            };

            for (var column = 1; column <= headers.Length; column++)
            {
                sheet.Cell(1, column).Value = headers[column - 1];
            }

            sheet.Range(1, 1, 1, headers.Length).Style
                .Font.SetBold()
                .Fill.SetBackgroundColor(XLColor.FromHtml("#E0E7FF"));

            sheet.Cell(2, 1).Value = 1;
            sheet.Cell(2, 2).Value = "Morning city tour";
            sheet.Cell(2, 3).Value = "Visit the city center and learn about local history.";
            sheet.Cell(2, 4).Value = "08:00";
            sheet.Cell(2, 5).Value = "10:30";
            sheet.Cell(2, 6).Value = "Ben Thanh Market";
            sheet.Cell(2, 7).Value = "";
            sheet.Cell(2, 8).Value = "";
            sheet.Cell(2, 9).Value = "";
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
                new[] { "DayNumber", "Yes", "Positive whole number." },
                new[] { "Title", "Yes", "Between 3 and 255 characters." },
                new[] { "Description", "Yes", "Between 10 and 2000 characters." },
                new[] { "StartTime", "Yes", "Use HH:mm format, for example 08:00." },
                new[] { "EndTime", "Yes", "Use HH:mm format and enter a time later than StartTime." },
                new[] { "LocationName", "Yes", "Between 3 and 255 characters." },
                new[] { "Latitude", "No", "May be left blank. Pick the precise location on the web before saving." },
                new[] { "Longitude", "No", "May be left blank. Pick the precise location on the web before saving." },
                new[] { "TourismName", "No", "Enter the exact tourism place name shown on StayHub, or leave blank." },
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

        private static void NormalizeAndValidate(BaseTourItineraryDTO dto)
        {
            dto.Title = dto.Title?.Trim();
            dto.Description = dto.Description?.Trim();
            dto.LocationName = dto.LocationName?.Trim();

            if (dto.DayNumber <= 0)
                throw new Exception("DayNumber must be greater than 0.");
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Length < 3 || dto.Title.Length > 255)
                throw new Exception("Title must be between 3 and 255 characters.");
            if (string.IsNullOrWhiteSpace(dto.Description) || dto.Description.Length < 10 || dto.Description.Length > 2000)
                throw new Exception("Description must be between 10 and 2000 characters.");
            if (string.IsNullOrWhiteSpace(dto.LocationName) || dto.LocationName.Length < 3 || dto.LocationName.Length > 255)
                throw new Exception("LocationName must be between 3 and 255 characters.");
            if (!dto.LocationLat.HasValue || !dto.LocationLng.HasValue)
                throw new Exception("Latitude and longitude are required.");
            if (dto.LocationLat is < -90 or > 90)
                throw new Exception("Latitude must be between -90 and 90.");
            if (dto.LocationLng is < -180 or > 180)
                throw new Exception("Longitude must be between -180 and 180.");
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
