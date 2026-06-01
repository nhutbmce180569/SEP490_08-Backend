namespace AIAPI.Helpers;

public record CityGeo(string DisplayName, double Latitude, double Longitude);

public static class VietnamCityGeoResolver
{
    private static readonly Dictionary<string, CityGeo> Cities = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cantho"] = new("Can Tho", 10.0452, 105.7469),
        ["cairang"] = new("Can Tho", 10.0035, 105.7823),
        ["dalat"] = new("Da Lat", 11.9404, 108.4583),
        ["hochiminh"] = new("Ho Chi Minh City", 10.8231, 106.6297),
        ["hanoi"] = new("Hanoi", 21.0285, 105.8542),
        ["danang"] = new("Da Nang", 16.0544, 108.2022),
        ["hoian"] = new("Hoi An", 15.8801, 108.3380),
        ["phuquoc"] = new("Phu Quoc", 10.2899, 103.9840),
        ["nhatrang"] = new("Nha Trang", 12.2388, 109.1967),
        ["halong"] = new("Quang Ninh", 20.9101, 107.1839),
        ["quangninh"] = new("Quang Ninh", 20.9101, 107.1839),
        ["sapa"] = new("Lao Cai", 22.3364, 103.8438),
        ["laocai"] = new("Lao Cai", 22.3364, 103.8438),
        ["ninhbinh"] = new("Ninh Binh", 20.2506, 105.9745),
        ["khanhhoa"] = new("Khanh Hoa", 11.9984, 109.2193),
        ["camranh"] = new("Cam Ranh", 11.9984, 109.2193),
        ["hue"] = new("Hue", 16.4637, 107.5909),
        ["mekong"] = new("Can Tho", 10.0452, 105.7469),
    };

    public static CityGeo? Resolve(string? cityOrRegion)
    {
        if (string.IsNullOrWhiteSpace(cityOrRegion))
        {
            return null;
        }

        var key = VietnameseTextNormalizer.Normalize(cityOrRegion);
        if (Cities.TryGetValue(key, out var exact))
        {
            return exact;
        }

        foreach (var pair in Cities)
        {
            if (key.Contains(pair.Key, StringComparison.Ordinal) || pair.Key.Contains(key, StringComparison.Ordinal))
            {
                return pair.Value;
            }
        }

        return null;
    }
}
