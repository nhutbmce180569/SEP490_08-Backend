using AutoMapper;
using Microsoft.Extensions.Options;
using PaymentAPI.DTOs;
using PaymentAPI.Models;
using PaymentAPI.Repositories;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PaymentAPI.Services.Implements
{
    public class MomoService : IMomoService
    {
        private const string ProviderName = "MoMo";

        private readonly MomoConfig _config;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IMapper _mapper;
        private readonly IBookingApiClient _bookingApiClient;
        private readonly HttpClient _httpClient;
        private readonly ILogger<MomoService> _logger;

        public MomoService(
            IOptions<MomoConfig> config,
            ITransactionRepository transactionRepository,
            IMapper mapper,
            IBookingApiClient bookingApiClient,
            HttpClient httpClient,
            ILogger<MomoService> logger)
        {
            _config = config.Value;
            _transactionRepository = transactionRepository;
            _mapper = mapper;
            _bookingApiClient = bookingApiClient;
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<MomoPaymentDTO> CreatePaymentAsync(
            CreateTransactionDTO transactionDto)
        {
            var transaction = _mapper.Map<Transaction>(transactionDto);
            transaction.Provider = ProviderName;
            transaction.Status = "Pending";

            var savedTransaction = await _transactionRepository.CreateAsync(transaction);
            var requestId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var momoOrderId = $"STAYHUB-{savedTransaction.Id}-{requestId}";
            var orderInfo = $"Payment for StayHub order {savedTransaction.OrderId}";
            var clientType = NormalizeClientType(transactionDto.ClientType);
            var extraData = Convert.ToBase64String(
                Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(new
                    {
                        transactionId = savedTransaction.Id,
                        clientType,
                        customerEmail = transactionDto.CustomerEmail
                    })));

            var rawHash =
                $"accessKey={_config.AccessKey}" +
                $"&amount={savedTransaction.Amount}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={_config.IpnUrl}" +
                $"&orderId={momoOrderId}" +
                $"&orderInfo={orderInfo}" +
                $"&partnerCode={_config.PartnerCode}" +
                $"&redirectUrl={_config.RedirectUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={_config.RequestType}";

            var requestBody = new
            {
                partnerCode = _config.PartnerCode,
                requestType = _config.RequestType,
                ipnUrl = _config.IpnUrl,
                redirectUrl = _config.RedirectUrl,
                orderId = momoOrderId,
                amount = savedTransaction.Amount,
                orderInfo,
                requestId,
                extraData,
                signature = HmacSHA256(_config.SecretKey, rawHash),
                lang = "vi"
            };

            using var response = await _httpClient.PostAsJsonAsync(
                _config.MomoApiUrl,
                requestBody);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                await MarkTransactionFailedAsync(savedTransaction);
                _logger.LogWarning(
                    "MoMo create payment failed for transaction {TransactionId}. Status: {StatusCode}. Response: {Response}",
                    savedTransaction.Id,
                    response.StatusCode,
                    responseContent);
                throw new InvalidOperationException("Could not create MoMo payment.");
            }

            using var momoResponse = JsonDocument.Parse(responseContent);
            var root = momoResponse.RootElement;
            var resultCode = root.TryGetProperty("resultCode", out var resultCodeElement)
                ? resultCodeElement.GetInt32()
                : -1;

            if (resultCode == 0 &&
                root.TryGetProperty("payUrl", out var payUrlElement))
            {
                var payUrl = payUrlElement.GetString();
                if (!string.IsNullOrWhiteSpace(payUrl))
                {
                    return new MomoPaymentDTO
                    {
                        PaymentUrl = payUrl,
                        Deeplink = GetOptionalString(root, "deeplink"),
                        QrCodeUrl = GetOptionalString(root, "qrCodeUrl")
                    };
                }
            }

            await MarkTransactionFailedAsync(savedTransaction);
            _logger.LogWarning(
                "MoMo rejected transaction {TransactionId}. Response: {Response}",
                savedTransaction.Id,
                responseContent);
            throw new InvalidOperationException("MoMo did not accept the payment request.");
        }

        public async Task<PaymentCallbackResultDTO> HandleMomoReturnAsync(
            IReadOnlyDictionary<string, string> values)
        {
            var metadata = ParseExtraData(GetValue(values, "extraData"));
            if (!ValidateSignature(values))
            {
                return new PaymentCallbackResultDTO
                {
                    Status = "Invalid signature",
                    ClientType = metadata.ClientType
                };
            }

            if (metadata.TransactionId <= 0)
            {
                return new PaymentCallbackResultDTO
                {
                    Status = "Invalid transaction id",
                    ClientType = metadata.ClientType
                };
            }

            var transaction =
                await _transactionRepository.GetByIdAsync(metadata.TransactionId);
            if (transaction == null)
            {
                return new PaymentCallbackResultDTO
                {
                    Status = "Transaction not found",
                    ClientType = metadata.ClientType
                };
            }

            if (!string.Equals(
                    transaction.Provider,
                    ProviderName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new PaymentCallbackResultDTO
                {
                    Status = "Invalid provider",
                    OrderId = transaction.OrderId.ToString(),
                    ClientType = metadata.ClientType
                };
            }

            transaction.ProviderTxnId = GetValue(values, "transId");
            transaction.Status = GetValue(values, "resultCode") == "0"
                ? "Success"
                : "Failed";
            await _transactionRepository.UpdateAsync(transaction);

            var orderUpdated = transaction.Status == "Success"
                ? await _bookingApiClient.MarkOrderPaidAsync(
                    transaction.OrderId,
                    metadata.CustomerEmail)
                : await _bookingApiClient.CancelOrderAsync(transaction.OrderId);

            return new PaymentCallbackResultDTO
            {
                Status = transaction.Status,
                OrderId = transaction.OrderId.ToString(),
                ClientType = metadata.ClientType
            };
        }

        public async Task<bool> ConfirmOrderPaymentAsync(int orderId)
        {
            var transaction =
                await _transactionRepository.GetByOrderIdAndProviderAsync(
                    orderId,
                    ProviderName);
            if (transaction == null || transaction.Status != "Success")
            {
                return false;
            }

            return await _bookingApiClient.MarkOrderPaidAsync(orderId, null);
        }

        public async Task<bool> CancelOrderPaymentAsync(int orderId)
        {
            var transaction =
                await _transactionRepository.GetByOrderIdAndProviderAsync(
                    orderId,
                    ProviderName);
            if (transaction?.Status == "Success")
            {
                return false;
            }

            return await _bookingApiClient.CancelOrderAsync(orderId);
        }

        private bool ValidateSignature(IReadOnlyDictionary<string, string> values)
        {
            var rawHash =
                $"accessKey={_config.AccessKey}" +
                $"&amount={GetValue(values, "amount")}" +
                $"&extraData={GetValue(values, "extraData")}" +
                $"&message={GetValue(values, "message")}" +
                $"&orderId={GetValue(values, "orderId")}" +
                $"&orderInfo={GetValue(values, "orderInfo")}" +
                $"&orderType={GetValue(values, "orderType")}" +
                $"&partnerCode={GetValue(values, "partnerCode")}" +
                $"&payType={GetValue(values, "payType")}" +
                $"&requestId={GetValue(values, "requestId")}" +
                $"&responseTime={GetValue(values, "responseTime")}" +
                $"&resultCode={GetValue(values, "resultCode")}" +
                $"&transId={GetValue(values, "transId")}";

            var computedSignature = HmacSHA256(_config.SecretKey, rawHash);
            return computedSignature.Equals(
                GetValue(values, "signature"),
                StringComparison.OrdinalIgnoreCase);
        }

        private async Task MarkTransactionFailedAsync(Transaction transaction)
        {
            transaction.Status = "Failed";
            await _transactionRepository.UpdateAsync(transaction);
        }

        private static string GetValue(
            IReadOnlyDictionary<string, string> values,
            string key)
        {
            return values.TryGetValue(key, out var value) ? value : string.Empty;
        }

        private static string? GetOptionalString(
            JsonElement root,
            string propertyName)
        {
            return root.TryGetProperty(propertyName, out var element)
                ? element.GetString()
                : null;
        }

        private static (
            int TransactionId,
            string ClientType,
            string? CustomerEmail) ParseExtraData(string extraData)
        {
            if (int.TryParse(extraData, out var legacyTransactionId))
            {
                return (legacyTransactionId, "web", null);
            }

            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(extraData));
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;
                var transactionId =
                    root.TryGetProperty("transactionId", out var idElement) &&
                    idElement.TryGetInt32(out var parsedId)
                        ? parsedId
                        : 0;
                var clientType = root.TryGetProperty(
                    "clientType",
                    out var clientTypeElement)
                    ? NormalizeClientType(clientTypeElement.GetString())
                    : "web";
                var customerEmail = root.TryGetProperty(
                    "customerEmail",
                    out var emailElement)
                    ? emailElement.GetString()
                    : null;
                return (transactionId, clientType, customerEmail);
            }
            catch (FormatException)
            {
                return (0, "web", null);
            }
            catch (JsonException)
            {
                return (0, "web", null);
            }
        }

        private static string NormalizeClientType(string? clientType)
        {
            return string.Equals(
                clientType,
                "mobile",
                StringComparison.OrdinalIgnoreCase)
                ? "mobile"
                : "web";
        }

        private static string HmacSHA256(string key, string rawData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var rawBytes = Encoding.UTF8.GetBytes(rawData);

            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(rawBytes);

            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
