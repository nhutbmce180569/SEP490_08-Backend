using BookingAPI.DTOs;
using BookingAPI.Models;
using BookingAPI.Repositories;
using Hangfire;
using System.Net;
using System.Text;

namespace BookingAPI.Services.Implements
{
    public class BackgroundJobService : IBackgroundJobService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ITourApiClient _tourApiClient;
        private readonly IEmailService _emailService;
        private readonly IQrCodeService _qrCodeService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BackgroundJobService> _logger;

        public BackgroundJobService(
            IOrderRepository orderRepository,
            ITourApiClient tourApiClient,
            IEmailService emailService,
            IQrCodeService qrCodeService,
            IConfiguration configuration,
            ILogger<BackgroundJobService> logger)
        {
            _orderRepository = orderRepository;
            _tourApiClient = tourApiClient;
            _emailService = emailService;
            _qrCodeService = qrCodeService;
            _configuration = configuration;
            _logger = logger;
        }

        public void ScheduleAutoCancelOrder(int orderId)
        {
            BackgroundJob.Schedule<IBackgroundJobService>(
                x => x.CancelOrderIfUnpaidAsync(orderId),
                TimeSpan.FromMinutes(17)
            );
        }

        public async Task CancelOrderIfUnpaidAsync(int orderId)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);

            if (order == null)
            {
                return;
            }

            if (order.Status != "Pending")
            {
                return;
            }

            var cancelled = await _orderRepository
                .CancelOrderWithTicketsAsync(orderId);

            if (cancelled)
            {
                await _tourApiClient.ReleaseScheduleSeatsAsync(
                    order.ScheduleId,
                    order.TicketCount
                );
            }
        }

        public void EnqueueSendTicketsEmail(int orderId, string customerEmail)
        {
            if (orderId <= 0 || string.IsNullOrWhiteSpace(customerEmail))
            {
                return;
            }

            BackgroundJob.Enqueue<IBackgroundJobService>(
                x => x.SendTicketsEmailForPaidOrderAsync(orderId, customerEmail));
        }

        public async Task SendTicketsEmailForPaidOrderAsync(int orderId, string customerEmail)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return;
            }

            if (order.Status != "Paid" && order.Status != "Completed")
            {
                return;
            }

            try
            {
                await SendTicketsEmailAsync(customerEmail, order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send ticket email for order {OrderId}", orderId);
                throw;
            }
        }

        private async Task SendTicketsEmailAsync(string customerEmail, Order order)
        {
            if (string.IsNullOrWhiteSpace(customerEmail) || !order.Tickets.Any())
            {
                return;
            }

            var schedule = await _tourApiClient.GetScheduleByIdAsync(order.ScheduleId);
            ReadOrderTourDTO? tour = null;
            if (schedule?.TourId > 0)
            {
                tour = await _tourApiClient.GetTourByIdAsync(schedule.TourId);
            }

            var subject = $"StayHub tickets for order #{order.Id}";
            var inlineImages = new List<EmailInlineImage>();
            var body = BuildTicketEmailBody(order, schedule, tour, inlineImages);
            await _emailService.SendEmailAsync(customerEmail, subject, body, inlineImages);
        }

        private string BuildTicketEmailBody(
            Order order,
            ReadOrderScheduleDTO? schedule,
            ReadOrderTourDTO? tour,
            List<EmailInlineImage> inlineImages)
        {
            var tourName = WebUtility.HtmlEncode(tour?.Name ?? "StayHub tour");
            var departureDate = schedule?.DepartureDate.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
            var returnDate = schedule?.ReturnDate.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
            var location = string.Join(", ", new[] { tour?.City, tour?.Country }
                .Where(x => !string.IsNullOrWhiteSpace(x)));
            var locationText = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(location) ? "N/A" : location);
            var finalAmount = $"{order.FinalAmount:N0} VND";
            var scheduleId = schedule?.Id.ToString() ?? order.ScheduleId.ToString();
            var pricePerTicket = schedule == null ? "N/A" : $"{schedule.Price:N0} VND";
            var orderDetailUrl = BuildOrderDetailUrl(order.Id);
            var safeOrderDetailUrl = WebUtility.HtmlEncode(orderDetailUrl);
            var html = new StringBuilder();

            html.Append($@"
<!DOCTYPE html>
<html>
<body style=""margin:0;padding:0;background:#eef2f7;color:#172033;font-family:Arial,Helvetica,sans-serif;"">
  <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background:#eef2f7;padding:28px 12px;"">
    <tr>
      <td align=""center"">
        <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:720px;background:#ffffff;border-collapse:collapse;border:1px solid #d9e2ef;"">
          <tr>
            <td style=""background:#0f766e;padding:24px 28px;color:#ffffff;"">
              <div style=""font-size:13px;letter-spacing:.08em;text-transform:uppercase;font-weight:700;"">StayHub</div>
              <h1 style=""margin:8px 0 0;font-size:24px;line-height:1.25;font-weight:700;"">Your e-tickets are ready</h1>
              <p style=""margin:8px 0 0;font-size:14px;color:#d9fffb;"">Order #{order.Id} has been paid successfully.</p>
            </td>
          </tr>
          <tr>
            <td style=""padding:24px 28px 8px;"">
              <h2 style=""margin:0 0 8px;font-size:20px;line-height:1.3;color:#111827;"">{tourName}</h2>
              <p style=""margin:0 0 18px;font-size:14px;color:#5b6472;"">Please show the QR code below at check-in.</p>

              <div style=""margin:0 0 18px;"">
                <a href=""{safeOrderDetailUrl}"" style=""display:inline-block;background:#0f766e;color:#ffffff;text-decoration:none;font-weight:700;font-size:14px;padding:11px 16px;"">View booking details</a>
              </div>

              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;background:#f8fafc;border:1px solid #e2e8f0;"">
                <tr>
                  <td style=""padding:14px 16px;border-bottom:1px solid #e2e8f0;width:50%;"">
                    <div style=""font-size:12px;color:#64748b;text-transform:uppercase;"">Departure</div>
                    <div style=""font-size:14px;color:#0f172a;font-weight:700;margin-top:4px;"">{departureDate}</div>
                  </td>
                  <td style=""padding:14px 16px;border-bottom:1px solid #e2e8f0;"">
                    <div style=""font-size:12px;color:#64748b;text-transform:uppercase;"">Return</div>
                    <div style=""font-size:14px;color:#0f172a;font-weight:700;margin-top:4px;"">{returnDate}</div>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:14px 16px;border-bottom:1px solid #e2e8f0;width:50%;"">
                    <div style=""font-size:12px;color:#64748b;text-transform:uppercase;"">Schedule</div>
                    <div style=""font-size:14px;color:#0f172a;font-weight:700;margin-top:4px;"">#{scheduleId}</div>
                  </td>
                  <td style=""padding:14px 16px;border-bottom:1px solid #e2e8f0;"">
                    <div style=""font-size:12px;color:#64748b;text-transform:uppercase;"">Price per ticket</div>
                    <div style=""font-size:14px;color:#0f172a;font-weight:700;margin-top:4px;"">{pricePerTicket}</div>
                  </td>
                </tr>
                <tr>
                  <td style=""padding:14px 16px;width:50%;"">
                    <div style=""font-size:12px;color:#64748b;text-transform:uppercase;"">Destination</div>
                    <div style=""font-size:14px;color:#0f172a;font-weight:700;margin-top:4px;"">{locationText}</div>
                  </td>
                  <td style=""padding:14px 16px;"">
                    <div style=""font-size:12px;color:#64748b;text-transform:uppercase;"">Tickets / Total</div>
                    <div style=""font-size:14px;color:#0f172a;font-weight:700;margin-top:4px;"">{order.TicketCount} ticket(s) - {finalAmount}</div>
                  </td>
                </tr>
              </table>
            </td>
          </tr>");

            foreach (var ticket in order.Tickets)
            {
                var scanUrl = BuildTicketScanUrl(ticket.QrCode);
                var qrContentId = $"ticket-{ticket.Id}-{Guid.NewGuid():N}";
                var qrBytes = _qrCodeService.GeneratePngBytes(scanUrl);
                var attendeeName = WebUtility.HtmlEncode(ticket.AttendeeName);
                var idCard = WebUtility.HtmlEncode(ticket.IdCard);
                var status = WebUtility.HtmlEncode(ticket.CheckInStatus ?? "Pending");
                var dateOfBirth = ticket.DateOfBirth?.ToString("dd/MM/yyyy") ?? "N/A";
                var gender = WebUtility.HtmlEncode(ticket.Gender ?? "N/A");
                var nationality = WebUtility.HtmlEncode(ticket.Nationality ?? "N/A");

                inlineImages.Add(new EmailInlineImage(
                    qrContentId,
                    $"ticket-{ticket.Id}.png",
                    "image/png",
                    qrBytes));

                html.Append($@"
          <tr>
            <td style=""padding:16px 28px;"">
              <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;border:1px solid #cbd5e1;background:#ffffff;"">
                <tr>
                  <td colspan=""2"" style=""padding:14px 16px;background:#f1f5f9;border-bottom:1px solid #cbd5e1;"">
                    <span style=""display:inline-block;font-size:12px;color:#0f766e;background:#ccfbf1;padding:4px 8px;font-weight:700;"">E-TICKET</span>
                    <span style=""font-size:15px;color:#111827;font-weight:700;margin-left:8px;"">Ticket #{ticket.Id}</span>
                    <span style=""float:right;font-size:12px;color:#64748b;"">{status}</span>
                  </td>
                </tr>
                <tr>
                  <td valign=""top"" style=""padding:16px;width:62%;"">
                    <div style=""font-size:12px;color:#64748b;text-transform:uppercase;"">Passenger</div>
                    <div style=""font-size:18px;color:#0f172a;font-weight:700;margin:4px 0 12px;"">{attendeeName}</div>

                    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""border-collapse:collapse;font-size:13px;color:#334155;"">
                      <tr>
                        <td style=""padding:6px 0;color:#64748b;width:38%;"">ID card</td>
                        <td style=""padding:6px 0;font-weight:700;"">{idCard}</td>
                      </tr>
                      <tr>
                        <td style=""padding:6px 0;color:#64748b;"">Date of birth</td>
                        <td style=""padding:6px 0;font-weight:700;"">{dateOfBirth}</td>
                      </tr>
                      <tr>
                        <td style=""padding:6px 0;color:#64748b;"">Gender</td>
                        <td style=""padding:6px 0;font-weight:700;"">{gender}</td>
                      </tr>
                      <tr>
                        <td style=""padding:6px 0;color:#64748b;"">Nationality</td>
                        <td style=""padding:6px 0;font-weight:700;"">{nationality}</td>
                      </tr>
                    </table>

                    <div style=""margin-top:16px;"">
                      <a href=""{safeOrderDetailUrl}"" style=""display:inline-block;background:#0f766e;color:#ffffff;text-decoration:none;font-weight:700;font-size:13px;padding:10px 14px;"">View booking detail</a>
                    </div>
                  </td>
                  <td valign=""top"" align=""center"" style=""padding:16px;width:38%;border-left:1px solid #e2e8f0;background:#fafafa;"">
                    <img src=""cid:{qrContentId}"" width=""168"" height=""168"" alt=""Ticket QR code"" style=""display:block;border:1px solid #cbd5e1;background:#ffffff;padding:8px;"" />
                    <div style=""font-size:12px;color:#64748b;margin-top:10px;"">Scan for check-in</div>
                  </td>
                </tr>
              </table>
            </td>
          </tr>");
            }

            html.Append(@"
          <tr>
            <td style=""padding:8px 28px 28px;"">
              <p style=""margin:0;font-size:12px;line-height:1.6;color:#64748b;"">This email contains your official StayHub ticket QR code. Keep it available on your phone and present it to staff before departure.</p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>");

            return html.ToString();
        }

        private string BuildTicketScanUrl(string? qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode))
            {
                return string.Empty;
            }

            var baseUrl = _configuration["GatewayApi:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return qrCode;
            }

            return $"{baseUrl.TrimEnd('/')}/api/tickets/scan/{Uri.EscapeDataString(qrCode)}";
        }

        private string BuildOrderDetailUrl(int orderId)
        {
            var baseUrl = _configuration["Frontend:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = "http://localhost:5173";
            }

            return $"{baseUrl.TrimEnd('/')}/my-bookings/{orderId}";
        }
    }
}
