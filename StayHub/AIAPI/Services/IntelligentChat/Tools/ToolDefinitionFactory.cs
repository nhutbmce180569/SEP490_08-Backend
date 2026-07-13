using System.Text.Json.Nodes;

namespace AIAPI.Services.IntelligentChat.Tools
{
    public class ToolDefinitionFactory
    {
        public JsonArray GetToolsDefinition()
        {
            return new JsonArray
            {
                new JsonObject
                {
                    ["functionDeclarations"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "recommend_tours_from_profile",
                            ["description"] = "Gợi ý tour du lịch cá nhân hóa dựa trên các sở thích, ngân sách, số lượng người, ngày đi và điểm đến mong muốn.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["companionType"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["enum"] = new JsonArray { "solo", "family", "couple", "group" },
                                        ["description"] = "Loại bạn đồng hành: solo (đi một mình), family (gia đình), couple (cặp đôi) hoặc group (nhóm bạn)."
                                    },
                                    ["preferredStartDate"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Ngày bắt đầu dự kiến (định dạng YYYY-MM-DD)."
                                    },
                                    ["preferredEndDate"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Ngày kết thúc dự kiến (định dạng YYYY-MM-DD)."
                                    },
                                    ["maxBudgetPerPerson"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Ngân sách tối đa cho một người (VND)."
                                    },
                                    ["travelPace"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["enum"] = new JsonArray { "relaxed", "moderate", "packed" },
                                        ["description"] = "Nhịp độ chuyến đi: relaxed (thư thả), moderate (vừa phải), packed (dày đặc)."
                                    },
                                    ["adultCount"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Số lượng người lớn."
                                    },
                                    ["childrenCount"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Số lượng trẻ em."
                                    },
                                    ["elderlyCount"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Số lượng người cao tuổi."
                                    },
                                    ["travelInterests"] = new JsonObject
                                    {
                                        ["type"] = "ARRAY",
                                        ["items"] = new JsonObject { ["type"] = "STRING" },
                                        ["description"] = "Danh sách các sở thích du lịch (ví dụ: ẩm thực, lịch sử, biển, núi, nghỉ dưỡng, khám phá)."
                                    },
                                    ["preferredCity"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Thành phố muốn đi du lịch ở Việt Nam (ví dụ: Đà Lạt, Nha Trang, Đà Nẵng, Huế, Sa Pa)."
                                    },
                                    ["preferredCountry"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Quốc gia muốn đi du lịch (mặc định: Việt Nam)."
                                    }
                                },
                                ["required"] = new JsonArray { "companionType", "preferredStartDate", "travelInterests", "preferredCity" }
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "search_tours",
                            ["description"] = "Tìm kiếm các tour du lịch dựa trên từ khóa tìm kiếm tự nhiên.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["query"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Từ khóa tìm kiếm (ví dụ: tour trekking, tour du thuyền)."
                                    },
                                    ["city"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Thành phố lọc kết quả (ví dụ: Đà Lạt, Nha Trang)."
                                    },
                                    ["minPrice"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Giá tối thiểu (VND)."
                                    },
                                    ["maxPrice"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Giá tối đa (VND)."
                                    },
                                    ["durationDays"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Số ngày của tour."
                                    }
                                },
                                ["required"] = new JsonArray { "query" }
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "search_tourism_insights",
                            ["description"] = "Tìm kiếm thông tin ẩm thực, văn hóa địa phương, đặc sản, danh lam thắng cảnh ở một thành phố nào đó.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["query"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Từ khóa cần tìm hiểu (ví dụ: đặc sản Đà Lạt, văn hóa ẩm thực Huế)."
                                    },
                                    ["city"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Tên thành phố cần tìm hiểu thông tin."
                                    }
                                },
                                ["required"] = new JsonArray { "query" }
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "get_weather_forecast",
                            ["description"] = "Xem thông tin dự báo thời tiết của một thành phố tại Việt Nam.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["city"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Tên thành phố cần xem thời tiết (ví dụ: Đà Lạt, Nha Trang, Đà Nẵng)."
                                    },
                                    ["startDate"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Ngày bắt đầu cần dự báo (định dạng YYYY-MM-DD)."
                                    },
                                    ["endDate"] = new JsonObject
                                    {
                                        ["type"] = "STRING",
                                        ["description"] = "Ngày kết thúc dự báo (định dạng YYYY-MM-DD, tùy chọn)."
                                    }
                                },
                                ["required"] = new JsonArray { "city" }
                            }
                        },
                        new JsonObject
                        {
                            ["name"] = "predict_hot_tours",
                            ["description"] = "Dự đoán các tour du lịch có thể hot (nhiều người đặt) trong tương lai dựa trên dữ liệu tương tác người dùng, đánh giá và tính thời vụ. Cung cấp gợi ý tour cụ thể và loại tour tiềm năng cho Manager.",
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "OBJECT",
                                ["properties"] = new JsonObject
                                {
                                    ["targetMonth"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Tháng dự đoán (1-12). Nếu không cung cấp, mặc định là tháng hiện tại hoặc tháng tới."
                                    },
                                    ["targetYear"] = new JsonObject
                                    {
                                        ["type"] = "INTEGER",
                                        ["description"] = "Năm dự đoán. Ví dụ: 2024, 2025."
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        public JsonArray GetOpenAiToolsDefinition()
        {
            var geminiTools = GetToolsDefinition();
            var openAiTools = new JsonArray();

            var declarations = geminiTools[0]?["functionDeclarations"]?.AsArray();
            if (declarations != null)
            {
                foreach (var decl in declarations)
                {
                    if (decl == null) continue;

                    var parameters = decl["parameters"]?.DeepClone();
                    if (parameters != null)
                    {
                        if (parameters["type"] != null)
                        {
                            parameters["type"] = parameters["type"]!.ToString().ToLower();
                        }
                        var properties = parameters["properties"]?.AsObject();
                        if (properties != null)
                        {
                            foreach (var prop in properties)
                            {
                                var propObj = prop.Value?.AsObject();
                                if (propObj != null && propObj["type"] != null)
                                {
                                    propObj["type"] = propObj["type"]!.ToString().ToLower();
                                }
                                if (propObj != null && propObj["items"]?.AsObject() is JsonObject itemsObj && itemsObj["type"] != null)
                                {
                                    itemsObj["type"] = itemsObj["type"]!.ToString().ToLower();
                                }
                            }
                        }
                    }

                    openAiTools.Add(new JsonObject
                    {
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = decl["name"]?.ToString(),
                            ["description"] = decl["description"]?.ToString(),
                            ["parameters"] = parameters
                        }
                    });
                }
            }
            return openAiTools;
        }
    }
}
