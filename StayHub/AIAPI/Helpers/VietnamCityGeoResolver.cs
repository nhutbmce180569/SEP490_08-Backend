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
        
        // Added missing cities from sample data
        ["aluoi"] = new("A Luoi", 16.2647, 107.2403),
        ["lagi"] = new("La Gi", 10.6667, 107.7667),
        ["vungliem"] = new("Vung Liem", 10.0833, 106.1667),
        ["quangbinh"] = new("Quang Binh", 17.4833, 106.6000),
        ["donghoi"] = new("Dong Hoi", 17.4833, 106.6000),
        ["phongnha"] = new("Phong Nha", 17.5833, 106.2833),
        ["quynhon"] = new("Quy Nhon", 13.7667, 109.2167),
        ["phuyen"] = new("Phu Yen", 13.0833, 109.3000),
        ["tuyhoa"] = new("Tuy Hoa", 13.0833, 109.3000),
        ["vungtau"] = new("Vung Tau", 10.3500, 107.0833),
        ["phanthiet"] = new("Phan Thiet", 10.9333, 108.1000),
        ["muine"] = new("Mui Ne", 10.9333, 108.2833),
        ["hagiang"] = new("Ha Giang", 22.8167, 104.9833),
        ["maichau"] = new("Mai Chau", 20.6667, 105.0833),
        ["mocchau"] = new("Moc Chau", 20.8500, 104.6333),
        ["daklak"] = new("Dak Lak", 12.6667, 108.0333),
        ["mangden"] = new("Mang Den", 14.6000, 108.2667),
        ["langco"] = new("Lang Co", 16.2500, 108.0333),
        ["bachma"] = new("Bach Ma", 16.1833, 107.8500),
        ["condao"] = new("Con Dao", 8.6833, 106.6000),
        ["lyson"] = new("Ly Son", 15.3833, 109.1167),
        ["tamcoc"] = new("Ninh Binh", 20.2167, 105.9333),
        ["chamislands"] = new("Hoi An", 15.9500, 108.5167),
        ["cattien"] = new("Dong Nai", 11.4167, 107.4167),
        ["trianlake"] = new("Dong Nai", 11.1667, 107.0000),
        ["dateh"] = new("Lam Dong", 11.5500, 107.5333),
        ["quychau"] = new("Nghe An", 19.5500, 105.0833),
        ["cualo"] = new("Nghe An", 18.8167, 105.7167),
        ["district1"] = new("Ho Chi Minh City", 10.7769, 106.7009),
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

        foreach (var pair in Cities.OrderByDescending(p => p.Key.Length))
        {
            if (key.Contains(pair.Key, StringComparison.Ordinal) || pair.Key.Contains(key, StringComparison.Ordinal))
            {
                return pair.Value;
            }
        }

        return null;
    }

    public static CityGeo? ResolveFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var normalized = VietnameseTextNormalizer.Normalize(text);
        foreach (var pair in Cities.OrderByDescending(p => p.Key.Length))
        {
            if (normalized.Contains(pair.Key, StringComparison.Ordinal))
            {
                return pair.Value;
            }
        }

        return null;
    }
}
