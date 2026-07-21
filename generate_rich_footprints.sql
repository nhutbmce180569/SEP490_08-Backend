-- ====================================================================================
-- STAYHUB RICH FOOTPRINT DATA GENERATOR
-- This script generates rich, continuous trace paths (LocationLogs) for testing.
-- It maps the waypoints of active bookings and interpolates intermediate points.
-- ====================================================================================

USE StayHub_SocialDb;
GO

PRINT '--- STARTING RICH FOOTPRINTS GENERATION ---';

-- Clear existing logs for targets first to avoid overlap
DELETE FROM LocationLogs WHERE UserId IN (63, 68, 95);

-- Targets definition
IF OBJECT_ID('tempdb..#Targets') IS NOT NULL DROP TABLE #Targets;
CREATE TABLE #Targets (
    UserId INT,
    ScheduleId INT
);

INSERT INTO #Targets VALUES
(63, 1438), (63, 1687), (63, 2139), (63, 2959),
(68, 760),
(95, 2078);

DECLARE @UserId INT, @ScheduleId INT;

DECLARE target_cursor CURSOR FOR
SELECT UserId, ScheduleId FROM #Targets;

OPEN target_cursor;
FETCH NEXT FROM target_cursor INTO @UserId, @ScheduleId;

WHILE @@FETCH_STATUS = 0
BEGIN
    PRINT 'Generating footprint path for User: ' + CAST(@UserId AS VARCHAR) + ', Schedule: ' + CAST(@ScheduleId AS VARCHAR);

    IF OBJECT_ID('tempdb..#Wps') IS NOT NULL DROP TABLE #Wps;
    
    SELECT 
        ROW_NUMBER() OVER(ORDER BY DayNumber, Id) as RowNum,
        LocationLat as Lat,
        LocationLng as Lng
    INTO #Wps
    FROM StayHub_CatalogDb.dbo.TourScheduleItineraries
    WHERE ScheduleId = @ScheduleId
      AND LocationLat IS NOT NULL AND LocationLng IS NOT NULL;

    DECLARE @TotalWps INT = (SELECT COUNT(*) FROM #Wps);
    DECLARE @i INT = 1;

    IF @TotalWps > 0
    BEGIN
        WHILE @i < @TotalWps
        BEGIN
            DECLARE @lat1 FLOAT, @lng1 FLOAT, @lat2 FLOAT, @lng2 FLOAT;
            SELECT @lat1 = Lat, @lng1 = Lng FROM #Wps WHERE RowNum = @i;
            SELECT @lat2 = Lat, @lng2 = Lng FROM #Wps WHERE RowNum = @i + 1;

            DECLARE @step INT = 0;
            DECLARE @stepsCount INT = 120; -- 120 interpolated points per segment
            
            WHILE @step <= @stepsCount
            BEGIN
                DECLARE @t FLOAT = CAST(@step AS FLOAT) / @stepsCount;
                DECLARE @curLat FLOAT = @lat1 + @t * (@lat2 - @lat1);
                DECLARE @curLng FLOAT = @lng1 + @t * (@lng2 - @lng1);

                -- Add minor GPS noise (jitter)
                SET @curLat = @curLat + (RAND() - 0.5) * 0.00015;
                SET @curLng = @curLng + (RAND() - 0.5) * 0.00015;

                INSERT INTO LocationLogs (ScheduleId, UserId, Lat, Lng, Timestamp)
                VALUES (@ScheduleId, @UserId, @curLat, @curLng, DATEADD(MINUTE, -(@stepsCount - @step + @i * 40) * 5, GETDATE()));

                SET @step = @step + 1;
            END

            SET @i = @i + 1;
        END

        IF @TotalWps = 1
        BEGIN
            DECLARE @baseLat FLOAT, @baseLng FLOAT;
            SELECT @baseLat = Lat, @baseLng = Lng FROM #Wps WHERE RowNum = 1;
            
            DECLARE @randStep INT = 0;
            WHILE @randStep < 100
            BEGIN
                DECLARE @offsetLat FLOAT = @baseLat + (RAND() - 0.5) * 0.01;
                DECLARE @offsetLng FLOAT = @baseLng + (RAND() - 0.5) * 0.01;

                INSERT INTO LocationLogs (ScheduleId, UserId, Lat, Lng, Timestamp)
                VALUES (@ScheduleId, @UserId, @offsetLat, @offsetLng, DATEADD(MINUTE, -@randStep * 5, GETDATE()));

                SET @randStep = @randStep + 1;
            END
        END
    END
    ELSE
    BEGIN
        -- Fallback if no waypoints exist for this schedule: simulate Ninh Binh route
        DECLARE @fallbackLat FLOAT = 20.21;
        DECLARE @fallbackLng FLOAT = 106.01;
        
        DECLARE @fbStep INT = 0;
        WHILE @fbStep < 150
        BEGIN
            DECLARE @fLat FLOAT = @fallbackLat + (CAST(@fbStep AS FLOAT) / 150.0) * 0.1 + (RAND() - 0.5) * 0.002;
            DECLARE @fLng FLOAT = @fallbackLng + (CAST(@fbStep AS FLOAT) / 150.0) * 0.05 + (RAND() - 0.5) * 0.002;

            INSERT INTO LocationLogs (ScheduleId, UserId, Lat, Lng, Timestamp)
            VALUES (@ScheduleId, @UserId, @fLat, @fLng, DATEADD(MINUTE, -@fbStep * 10, GETDATE()));

            SET @fbStep = @fbStep + 1;
        END
    END

    FETCH NEXT FROM target_cursor INTO @UserId, @ScheduleId;
END;

CLOSE target_cursor;
DEALLOCATE target_cursor;

DROP TABLE #Targets;
PRINT '--- RICH FOOTPRINTS GENERATION COMPLETE ---';
GO
