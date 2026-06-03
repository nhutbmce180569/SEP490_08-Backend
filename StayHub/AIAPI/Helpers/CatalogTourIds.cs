namespace AIAPI.Helpers;

/// <summary>
/// Augmented catalog variants use synthetic IDs: baseId * 10_000 + variantIndex.
/// Public TourAPI only exposes base tour IDs.
/// </summary>
public static class CatalogTourIds
{
    public const int SyntheticIdMultiplier = 10_000;

    public static bool IsSynthetic(int tourId) => tourId >= SyntheticIdMultiplier;

    public static int ResolveBaseTourId(int tourId) =>
        IsSynthetic(tourId) ? tourId / SyntheticIdMultiplier : tourId;
}
