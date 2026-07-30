using System.Net.Http.Json;
using BookingAPI.DTOs;

namespace BookingAPI.Services.Implements;

public class VoucherApiClient : IVoucherApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public VoucherApiClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<ApplyVoucherResultDTO> RedeemVoucherAsync(ApplyVoucherRequest request)
    {
        if (_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("Voucher API base address is not configured.");
        }

        var response = await _httpClient.PostAsJsonAsync("api/customer/vouchers/redeem", request);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ApplyVoucherResultDTO>();
            if (result == null)
            {
                throw new InvalidOperationException("Voucher API returned an empty response.");
            }

            return result;
        }

        var error = await response.Content.ReadFromJsonAsync<VoucherApiErrorResponse>();
        throw new InvalidOperationException(error?.Message ?? "Failed to redeem voucher.");
    }

    public async Task RestoreVoucherAsync(int customerId, string voucherCode)
    {
        if (_httpClient.BaseAddress == null)
        {
            throw new InvalidOperationException("Voucher API base address is not configured.");
        }

        var serviceKey = _configuration["InternalService:Key"];
        if (string.IsNullOrWhiteSpace(serviceKey))
        {
            throw new InvalidOperationException("Internal service key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/internal/vouchers/restore");
        request.Headers.TryAddWithoutValidation("X-Service-Key", serviceKey);
        request.Content = JsonContent.Create(new RestoreVoucherRequest
        {
            CustomerId = customerId,
            Code = voucherCode
        });

        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var error = await response.Content.ReadFromJsonAsync<VoucherApiErrorResponse>();
        throw new InvalidOperationException(error?.Message ?? "Failed to restore voucher.");
    }
}
