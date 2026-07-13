using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using AIAPI.DTOs;
using AIAPI.Services;
using AIAPI.Services.IntelligentChat.Models;
using Microsoft.Extensions.Logging;

namespace AIAPI.Services.IntelligentChat.Tools
{
    public class PredictHotToursExecutor : IToolExecutor
    {
        private readonly ICatalogStore _catalogStore;
        private readonly ILogger<PredictHotToursExecutor> _logger;

        public PredictHotToursExecutor(
            ICatalogStore catalogStore,
            ILogger<PredictHotToursExecutor> logger)
        {
            _catalogStore = catalogStore;
            _logger = logger;
        }

        public string FunctionName => "predict_hot_tours";

        public Task<ToolExecutionResult> ExecuteAsync(JsonObject? args, CancellationToken cancellationToken)
        {
            int targetMonth = DateTime.Now.Month;
            if (args != null && args.TryGetPropertyValue("targetMonth", out var mNode) && mNode != null)
            {
                targetMonth = mNode.GetValue<int>();
            }
            
            _logger.LogInformation("Predicting hot tours for month {TargetMonth}...", targetMonth);

            var allTours = _catalogStore.Tours;
            if (allTours == null || !allTours.Any())
            {
                return Task.FromResult(new ToolExecutionResult
                {
                    ResponseData = new JsonObject { ["error"] = "Không có dữ liệu tour trong hệ thống." }
                });
            }

            // Giả lập thuật toán dự đoán:
            // Lấy dữ liệu tương tác từ ReviewCount, AverageStar, và kết hợp với tính mùa vụ.
            // Ví dụ: tháng hè (5, 6, 7) thì tour biển (Nha Trang, Đà Nẵng, Phú Quốc) sẽ hot.
            // Tháng đông (10, 11, 12) thì tour Sapa, Đà Lạt sẽ hot.
            
            bool isSummer = targetMonth >= 5 && targetMonth <= 8;
            bool isWinter = targetMonth >= 10 || targetMonth <= 1;
            bool isSpring = targetMonth >= 2 && targetMonth <= 4;

            var scoredTours = allTours.Select(t =>
            {
                double score = (t.ReviewCount * 0.7) + ((t.AverageStar ?? 0) * 20.0);
                
                string searchDoc = (t.SearchDocument ?? "").ToLower();
                string city = (t.City ?? "").ToLower();
                string name = (t.Name ?? "").ToLower();

                // Bonus mùa vụ
                if (isSummer && (searchDoc.Contains("biển") || city.Contains("nha trang") || city.Contains("đà nẵng") || city.Contains("phú quốc")))
                {
                    score += 150; 
                }
                if (isWinter && (searchDoc.Contains("lạnh") || searchDoc.Contains("sương") || city.Contains("đà lạt") || city.Contains("sa pa") || city.Contains("sapa")))
                {
                    score += 150;
                }
                if (isSpring && (searchDoc.Contains("hoa") || searchDoc.Contains("xuân") || searchDoc.Contains("chùa") || city.Contains("huế") || city.Contains("hà nội")))
                {
                    score += 150;
                }

                // Giả lập thêm một chút ngẫu nhiên cho trend mới nổi
                score += new Random().Next(0, 50);

                return new { Tour = t, Score = score };
            }).OrderByDescending(x => x.Score).ToList();

            var topTours = scoredTours.Take(5).ToList();

            var recommendations = topTours.Select(x => new TourRecommendationItemDTO
            {
                TourId = x.Tour.Id,
                Name = x.Tour.Name,
                City = x.Tour.City ?? "",
                ImageUrl = x.Tour.ImageUrl,
                MinPrice = x.Tour.MinPrice,
                DurationDays = x.Tour.DurationDays ?? 0,
                Reason = $"Điểm dự đoán độ hot: {Math.Round(x.Score)}, Lượt tương tác (đánh giá): {x.Tour.ReviewCount}, Đánh giá sao: {x.Tour.AverageStar}. Rất phù hợp cho tháng {targetMonth}."
            }).ToList();

            string suggestedType = isSummer ? "Tour biển, nghỉ dưỡng, thể thao dưới nước" :
                                   isWinter ? "Tour ngắm tuyết, săn mây, đón không khí lạnh" :
                                   isSpring ? "Tour ngắm hoa, du xuân, lễ hội chùa chiền" : "Tour khám phá văn hóa, ẩm thực";

            var responseData = new JsonObject
            {
                ["targetMonth"] = targetMonth,
                ["suggestedTourType"] = suggestedType,
                ["reason"] = $"Tháng {targetMonth} thường có xu hướng tìm kiếm các " + suggestedType.ToLower() + " do điều kiện thời tiết và chu kỳ nghỉ lễ.",
                ["predictedHotToursCount"] = topTours.Count,
                ["predictedTours"] = new JsonArray(topTours.Select(t => new JsonObject
                {
                    ["id"] = t.Tour.Id,
                    ["name"] = t.Tour.Name,
                    ["trendScore"] = Math.Round(t.Score),
                    ["reviewCount"] = t.Tour.ReviewCount,
                    ["averageStar"] = t.Tour.AverageStar
                }).ToArray())
            };

            return Task.FromResult(new ToolExecutionResult
            {
                ResponseData = responseData,
                Recommendations = recommendations
            });
        }
    }
}
