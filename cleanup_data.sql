-- ====================================================================================
-- STAYHUB DATABASE CLEANUP SCRIPT FOR TOUR MOMENTS (WITH FOREIGN KEY CONSTRAINT HANDLING)
-- This script cleans up historical seed data that violates:
-- 1. Moments posted before the tour departure date.
-- 2. Moments posted with coordinates further than 50km from all tour route waypoints.
-- ====================================================================================

USE StayHub_SocialDb;
GO

PRINT '--- STARTING TOUR MOMENTS DATA CLEANUP ---';

-- 1. Clean up moments posted before the tour departed
DECLARE @BeforeDepartureCount INT;

SELECT @BeforeDepartureCount = COUNT(*)
FROM TourMoments m
JOIN StayHub_CatalogDb.dbo.TourSchedules s ON m.ScheduleId = s.Id
WHERE m.ScheduleId > 0
  AND m.CreatedAt < s.DepartureDate;

IF @BeforeDepartureCount > 0
BEGIN
    PRINT 'Found ' + CAST(@BeforeDepartureCount AS VARCHAR) + ' moments posted before their tour departed. Cleaning them up...';
    
    -- Delete reactions
    DELETE r
    FROM MomentReactions r
    JOIN TourMoments m ON r.MomentId = m.Id
    JOIN StayHub_CatalogDb.dbo.TourSchedules s ON m.ScheduleId = s.Id
    WHERE m.ScheduleId > 0 AND m.CreatedAt < s.DepartureDate;

    -- Delete comments
    DELETE c
    FROM MomentComments c
    JOIN TourMoments m ON c.MomentId = m.Id
    JOIN StayHub_CatalogDb.dbo.TourSchedules s ON m.ScheduleId = s.Id
    WHERE m.ScheduleId > 0 AND m.CreatedAt < s.DepartureDate;

    -- Delete moments
    DELETE m
    FROM TourMoments m
    JOIN StayHub_CatalogDb.dbo.TourSchedules s ON m.ScheduleId = s.Id
    WHERE m.ScheduleId > 0
      AND m.CreatedAt < s.DepartureDate;
      
    PRINT 'Successfully deleted moments posted before tour departure.';
END
ELSE
BEGIN
    PRINT 'No moments found posted before tour departure.';
END

GO

-- 2. Clean up moments with coordinates too far from the tour waypoints (> 50km)
DECLARE @MismatchedLocationCount INT;

SELECT @MismatchedLocationCount = COUNT(*)
FROM TourMoments m
WHERE m.ScheduleId > 0
  AND m.Lat IS NOT NULL AND m.Lng IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 
      FROM StayHub_CatalogDb.dbo.TourScheduleItineraries i
      WHERE i.ScheduleId = m.ScheduleId
        AND i.LocationLat IS NOT NULL AND i.LocationLng IS NOT NULL
        AND geography::Point(i.LocationLat, i.LocationLng, 4326).STDistance(geography::Point(m.Lat, m.Lng, 4326)) <= 50000
  )
  AND EXISTS (
      SELECT 1 
      FROM StayHub_CatalogDb.dbo.TourScheduleItineraries i
      WHERE i.ScheduleId = m.ScheduleId
        AND i.LocationLat IS NOT NULL AND i.LocationLng IS NOT NULL
  );

IF @MismatchedLocationCount > 0
BEGIN
    PRINT 'Found ' + CAST(@MismatchedLocationCount AS VARCHAR) + ' moments posted outside the 50km tour route boundary. Cleaning them up...';
    
    -- Store matching IDs in a temporary table for cleaner deletion
    IF OBJECT_ID('tempdb..#MismatchedMoments') IS NOT NULL DROP TABLE #MismatchedMoments;
    
    SELECT m.Id
    INTO #MismatchedMoments
    FROM TourMoments m
    WHERE m.ScheduleId > 0
      AND m.Lat IS NOT NULL AND m.Lng IS NOT NULL
      AND NOT EXISTS (
          SELECT 1 
          FROM StayHub_CatalogDb.dbo.TourScheduleItineraries i
          WHERE i.ScheduleId = m.ScheduleId
            AND i.LocationLat IS NOT NULL AND i.LocationLng IS NOT NULL
            AND geography::Point(i.LocationLat, i.LocationLng, 4326).STDistance(geography::Point(m.Lat, m.Lng, 4326)) <= 50000
      )
      AND EXISTS (
          SELECT 1 
          FROM StayHub_CatalogDb.dbo.TourScheduleItineraries i
          WHERE i.ScheduleId = m.ScheduleId
            AND i.LocationLat IS NOT NULL AND i.LocationLng IS NOT NULL
      );

    -- Delete reactions
    DELETE r
    FROM MomentReactions r
    WHERE r.MomentId IN (SELECT Id FROM #MismatchedMoments);

    -- Delete comments
    DELETE c
    FROM MomentComments c
    WHERE c.MomentId IN (SELECT Id FROM #MismatchedMoments);

    -- Delete moments
    DELETE m
    FROM TourMoments m
    WHERE m.Id IN (SELECT Id FROM #MismatchedMoments);
    
    DROP TABLE #MismatchedMoments;
    
    PRINT 'Successfully deleted moments posted outside 50km boundary.';
END
ELSE
BEGIN
    PRINT 'No moments found with mismatched locations (outside 50km boundary).';
END

PRINT '--- DATA CLEANUP COMPLETE ---';
GO
