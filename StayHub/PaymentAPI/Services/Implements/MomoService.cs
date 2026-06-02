using AutoMapper;
using Microsoft.AspNetCore.Http;
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

        public async Task<string> CreatePaymentUrlAsync(CreateTransactionDTO transactionDto)
        {
            var transaction = _mapper.Map<Transaction>(transactionDto);
            transaction.Provider = ProviderName;
            transaction.Status = "Pending";

            var savedTransaction = await _transactionRepository.CreateAsync(transaction);

            var requestId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
            var momoOrderId = $"STAYHUB-{savedTransaction.Id}-{requestId}";
            var orderInfo = $"Payment for StayHub order {savedTransaction.OrderId}";
            var extraData = savedTransaction.Id.ToString();

            var rawHash =
                $"partnerCode={_config.PartnerCode}" +
                $"&accessKey={_config.AccessKey}" +
                $"&requestId={requestId}" +
                $"&amount={savedTransaction.Amount}" +
                $"&orderId={momoOrderId}" +
                $"&orderInfo={orderInfo}" +
                $"&returnUrl={_config.ReturnUrl}" +
                $"&notifyUrl={_config.NotifyUrl}" +
                $"&extraData={extraData}";

            var requestBody = new
            {
                accessKey = _config.AccessKey,
                partnerCode = _config.PartnerCode,
                requestType = _config.RequestType,
                notifyUrl = _config.NotifyUrl,
                returnUrl = _config.ReturnUrl,
                orderId = momoOrderId,
                amount = savedTransaction.Amount.ToString(),
                orderInfo,
                requestId,
                extraData,
                signature = HmacSHA256(_config.SecretKey, rawHash)
            };

            using var response = await _httpClient.PostAsJsonAsync(_config.MomoApiUrl, requestBody);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                savedTransaction.Status = "Failed";
                await _transactionRepository.UpdateAsync(savedTransaction);

                _logger.LogWarning(
                    "MoMo create payment failed for transaction {TransactionId}. Status: {StatusCode}. Response: {Response}",
                    savedTransaction.Id,
                    response.StatusCode,
                    responseContent);

                throw new InvalidOperationException("Could not create MoMo payment URL.");
            }

            using var momoResponse = JsonDocument.Parse(responseContent);
            if (momoResponse.RootElement.TryGetProperty("payUrl", out var payUrlElement))
            {
                var payUrl = payUrlElement.GetString();
                if (!string.IsNullOrWhiteSpace(payUrl))
                {
                    return payUrl;
                }
            }

            savedTransaction.Status = "Failed";
            await _transactionRepository.UpdateAsync(savedTransaction);

            _logger.LogWarning(
                "MoMo response did not contain payUrl for transaction {TransactionId}. Response: {Response}",
                savedTransaction.Id,
                responseContent);

            throw new InvalidOperationException("MoMo response did not contain a payment URL.");
        }

        public async Task<(string Status, string? OrderId)> HandleMomoReturnAsync(IQueryCollection query)
        {
            if (!ValidateSignature(query))
            {
                return ("Invalid signature", null);
            }

            var transactionIdText = query["extraData"].ToString();
            if (!int.TryParse(transactionIdText, out var transactionId))
            {
                return ("Invalid transaction id", null);
            }

            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                return ("Transaction not found", null);
            }

            if (!string.Equals(transaction.Provider, ProviderName, StringComparison.OrdinalIgnoreCase))
            {
                return ("Invalid provider", transaction.OrderId.ToString());
            }

            var resultCode = query["resultCode"].ToString();
            if (string.IsNullOrWhiteSpace(resultCode))
            {
                resultCode = query["errorCode"].ToString();
            }

            transaction.ProviderTxnId = query["transId"].ToString();
            transaction.Status = resultCode == "0" ? "Success" : "Failed";
            await _transactionRepository.UpdateAsync(transaction);

            return (transaction.Status, transaction.OrderId.ToString());
        }

        public async Task<bool> ConfirmOrderPaymentAsync(int orderId)
        {
            var transaction = await _transactionRepository.GetByOrderIdAndProviderAsync(orderId, ProviderName);
            if (transaction == null || transaction.Status != "Success")
            {
                return false;
            }

            return await _bookingApiClient.MarkOrderPaidAsync(orderId);
        }

        public async Task<bool> CancelOrderPaymentAsync(int orderId)
        {
            var transaction = await _transactionRepository.GetByOrderIdAndProviderAsync(orderId, ProviderName);
            if (transaction?.Status == "Success")
            {
                return false;
            }

            return await _bookingApiClient.CancelOrderAsync(orderId);
        }

        private bool ValidateSignature(IQueryCollection query)
        {
            var partnerCode = query["partnerCode"].ToString();
            var requestId = query["requestId"].ToString();
            var amount = query["amount"].ToString();
            var orderId = query["orderId"].ToString();
            var orderInfo = query["orderInfo"].ToString();
            var orderType = query["orderType"].ToString();
            var transId = query["transId"].ToString();
            var message = query["message"].ToString();
            var localMessage = query["localMessage"].ToString();
            var responseTime = query["responseTime"].ToString();
            var resultCode = query["resultCode"].ToString();
            if (string.IsNullOrWhiteSpace(resultCode))
            {
                resultCode = query["errorCode"].ToString();
            }

            var payType = query["payType"].ToString();
            var extraData = query["extraData"].ToString();
            var momoSignature = query["signature"].ToString();

            var rawHash =
                $"partnerCode={partnerCode}" +
                $"&accessKey={_config.AccessKey}" +
                $"&requestId={requestId}" +
                $"&amount={amount}" +
                $"&orderId={orderId}" +
                $"&orderInfo={orderInfo}" +
                $"&orderType={orderType}" +
                $"&transId={transId}" +
                $"&message={message}" +
                $"&localMessage={localMessage}" +
                $"&responseTime={responseTime}" +
                $"&errorCode={resultCode}" +
                $"&payType={payType}" +
                $"&extraData={extraData}";

            var computedSignature = HmacSHA256(_config.SecretKey, rawHash);
            return computedSignature.Equals(momoSignature, StringComparison.OrdinalIgnoreCase);
        }

        private static string HmacSHA256(string key, string rawData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var rawBytes = Encoding.UTF8.GetBytes(rawData);

            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(rawBytes);

            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
