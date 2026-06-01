-- Seed evaluation ground truth (chạy sau StayHub_AiDb_Patch.sql)
USE StayHub_AiDb;
GO

IF NOT EXISTS (SELECT 1 FROM UserTourInteractions WHERE Id = 7)
BEGIN
    SET IDENTITY_INSERT UserTourInteractions ON;
    INSERT INTO UserTourInteractions (Id, CustomerId, TourId, InteractionType, Weight, SessionId, CreatedAt) VALUES
    (7, 8, 4, 'view', 1, 'seed-session-005', DATEADD(DAY, -6, GETUTCDATE())),
    (8, 8, 4, 'click', 1.5, 'seed-session-005', DATEADD(DAY, -6, GETUTCDATE())),
    (9, 9, 4, 'wishlist', 4, 'seed-session-006', DATEADD(DAY, -5, GETUTCDATE())),
    (10, 10, 1, 'booking', 5, 'seed-session-007', DATEADD(DAY, -4, GETUTCDATE())),
    (11, NULL, 12, 'click', 2, 'anon-session-008', DATEADD(DAY, -3, GETUTCDATE())),
    (12, NULL, 12, 'wishlist', 4, 'anon-session-008', DATEADD(DAY, -3, GETUTCDATE()));
    SET IDENTITY_INSERT UserTourInteractions OFF;
    PRINT 'Inserted extra UserTourInteractions (7-12)';
END
ELSE
    PRINT 'UserTourInteractions seed already applied';
GO

IF NOT EXISTS (SELECT 1 FROM TourRelevanceJudgments)
BEGIN
    INSERT INTO TourRelevanceJudgments (ProfileSignature, ProfileQueryKey, TourId, RelevanceGrade, Source, JudgeId, Notes, CreatedAt) VALUES
    ('seed-cantho-river', 'foreigner_can_tho_couple_river', 5, 3, 'expert', 'reviewer-01', N'Mekong floating market matches river + culture for foreign couples', GETUTCDATE()),
    ('seed-cantho-river', 'foreigner_can_tho_couple_river', 9, 0, 'expert', 'reviewer-01', N'Saigon food tour irrelevant for Can Tho river trip', GETUTCDATE()),
    ('seed-phuquoc-beach', 'vietnamese_phu_quoc_solo_beach', 1, 3, 'expert', 'reviewer-01', N'Phu Quoc beach resort fit', GETUTCDATE()),
    ('seed-phuquoc-beach', 'vietnamese_phu_quoc_solo_beach', 6, 0, 'expert', 'reviewer-01', N'Sapa trek poor fit for beach solo', GETUTCDATE()),
    ('seed-hoian-culture', 'vietnamese_hoi_an_family_culture', 3, 3, 'expert', 'reviewer-02', N'Ancient town + lanterns for family culture trip', GETUTCDATE()),
    ('seed-hoian-culture', 'vietnamese_hoi_an_family_culture', 9, 1, 'expert', 'reviewer-02', N'Food tour weak match vs Hoi An focus', GETUTCDATE()),
    ('seed-dalat-nature', 'foreigner_da_lat_couple_nature', 2, 3, 'expert', 'reviewer-02', N'Cloud hunting / nature in Da Lat', GETUTCDATE()),
    ('seed-dalat-nature', 'foreigner_da_lat_couple_nature', 10, 1, 'expert', 'reviewer-02', N'Honeymoon resort partial overlap only', GETUTCDATE()),
    ('seed-hanoi-city', 'vietnamese_hanoi_solo_city', 11, 3, 'expert', 'reviewer-03', N'Photo walk Old Quarter', GETUTCDATE()),
    ('seed-halong-relax', 'vietnamese_quang_ninh_couple_relax', 4, 3, 'expert', 'reviewer-03', N'Luxury cruise matches relax + couple', GETUTCDATE());
    PRINT 'Inserted 10 TourRelevanceJudgments';
END
ELSE
    PRINT 'TourRelevanceJudgments seed already applied';
GO
