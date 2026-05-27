using System;
using System.Collections.Generic;

namespace SystemAPI.Models;

public partial class SystemSetting
{
    public int Id { get; set; }

    public string SettingKey { get; set; } = null!;

    public string SettingValue { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
