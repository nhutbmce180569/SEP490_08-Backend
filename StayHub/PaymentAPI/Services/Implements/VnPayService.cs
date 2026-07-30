using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using PaymentAPI.DTOs;
using PaymentAPI.Models;
using PaymentAPI.Repositories;
using PaymentAPI.Services;

namespace PaymentAPI.Services.Implements
{
    public class VnPayService : IVnPayService
    {
        private readonly VnPayConfig _config;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IMapper _mapper;
        private readonly IBookingApiClient _bookingApiClient;

        public VnPayService(
            IOptions<VnPayConfig> config,
            ITransactionRepository transactionRepository,
            IMapper mapper,
            IBookingApiClient bookingApiClient)
        {
            _config = config.Value;
            _transactionRepository = transactionRepository;
            _mapper = mapper;
            _bookingApiClient = bookingApiClient;
        }

        public async Task<string> CreatePaymentUrl(CreateTransactionDTO transactionDto, string? ipAddress)
        {
            var transaction = _mapper.Map<Transaction>(transactionDto);
            transaction.Provider = "VNPay";
            var savedTransaction = await _transactionRepository.CreateAsync(transaction);

            var vnpUrl = _config.BaseUrl;
            var tmnCode = _config.TmnCode;
            var hashSecret = _config.HashSecret;
            var returnUrl = _config.ReturnUrl;
            var clientType = NormalizeClientType(transactionDto.ClientType);
            var paymentMetadata = BuildPaymentMetadata(
                clientType,
                transactionDto.CustomerEmail);

            var vnpayData = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", tmnCode },
                { "vnp_Amount", ((long)(savedTransaction.Amount * 100)).ToString() }, // Bắt buộc nhân 100
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress ?? "127.0.0.1" },
                { "vnp_Locale", "en" },
                { "vnp_OrderInfo", paymentMetadata },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_TxnRef", savedTransaction.Id.ToString() } // Dùng Transaction Id thay vì OrderId làm mã tham chiếu
            };

            var query = string.Join("&", vnpayData.Select(kvp => $"{kvp.Key}={System.Net.WebUtility.UrlEncode(kvp.Value)}"));
            var secureHash = HmacSHA512(hashSecret, query);

            return $"{vnpUrl}?{query}&vnp_SecureHash={secureHash}";
        }

        private bool ValidateSignature(IQueryCollection query, string rawQuery)
        {
            var hashSecret = _config.HashSecret;
            var vnp_SecureHash = query["vnp_SecureHash"].ToString();
            var queryString = rawQuery.TrimStart('?');

            var filtered = string.Join("&", queryString.Split('&').Where(x => !x.StartsWith("vnp_SecureHash")).OrderBy(x => x));
            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(hashSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(filtered));

            var computedHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            return computedHash.Equals(vnp_SecureHash, StringComparison.OrdinalIgnoreCase);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
            }
        }

        public async Task<PaymentCallbackResultDTO> HandleVnPayReturnAsync(
            IQueryCollection query,
            string rawQuery)
        {
            var metadata = ParsePaymentMetadata(query["vnp_OrderInfo"].ToString());
            if (!ValidateSignature(query, rawQuery))
            {
                return new PaymentCallbackResultDTO
                {
                    Status = "Invalid signature",
                    ClientType = metadata.ClientType
                };
            }

            var responseCode = query["vnp_ResponseCode"].ToString();
            var transactionStatus = query["vnp_TransactionStatus"].ToString();
            var txnRefStr = query["vnp_TxnRef"].ToString();
            var vnpTransactionNo = query["vnp_TransactionNo"].ToString();

            if (!int.TryParse(txnRefStr, out int transactionId))
            {
                return new PaymentCallbackResultDTO
                {
                    Status = "Invalid transaction id",
                    ClientType = metadata.ClientType
                };
            }

            var transaction = await _transactionRepository.GetByIdAsync(transactionId);
            if (transaction == null)
            {
                return new PaymentCallbackResultDTO
                {
                    Status = "Transaction not found",
                    ClientType = metadata.ClientType
                };
            }

            transaction.ProviderTxnId = vnpTransactionNo;

            // Cập nhật trạng thái Payment theo kết quả VNPAY
            transaction.Status = (responseCode == "00" && transactionStatus == "00") ? "Success" : "Failed";
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
            var transaction = await _transactionRepository.GetByOrderIdAndProviderAsync(orderId, "VNPay");
            if (transaction == null || transaction.Status != "Success")
            {
                return false;
            }

            return await _bookingApiClient.MarkOrderPaidAsync(orderId, null);
        }

        public async Task<bool> CancelOrderPaymentAsync(int orderId)
        {
            var transaction = await _transactionRepository.GetByOrderIdAndProviderAsync(orderId, "VNPay");
            if (transaction?.Status == "Success")
            {
                return false;
            }

            return await _bookingApiClient.CancelOrderAsync(orderId);
        }

        private static string BuildPaymentMetadata(
            string clientType,
            string? customerEmail)
        {
            var email = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes(customerEmail ?? string.Empty))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
            return $"StayHub|{clientType}|{email}";
        }

        private static (string ClientType, string? CustomerEmail) ParsePaymentMetadata(
            string orderInfo)
        {
            var parts = orderInfo.Split('|');
            if (parts.Length != 3 || parts[0] != "StayHub")
            {
                return ("web", null);
            }

            var clientType = NormalizeClientType(parts[1]);
            try
            {
                var base64 = parts[2].Replace('-', '+').Replace('_', '/');
                base64 = base64.PadRight(
                    base64.Length + ((4 - base64.Length % 4) % 4),
                    '=');
                var email = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                return (clientType, string.IsNullOrWhiteSpace(email) ? null : email);
            }
            catch (FormatException)
            {
                return (clientType, null);
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
    }
}
