namespace VoucherAPI.Services;

public interface ITourValidationService
{
    Task<(bool Exists, string? Name, string? Status, int CreatedBy)> ValidateTourAsync(int tourId);
}
