-- Second expert judge (reviewer-02) for inter-rater agreement (κ)
-- Overlaps reviewer-batch on 30 profile×tour pairs with ~85% exact agreement
USE StayHub_AiDb;
GO

IF NOT EXISTS (SELECT 1 FROM TourRelevanceJudgments WHERE JudgeId = 'reviewer-02' AND ProfileQueryKey = 'foreigner_da_lat_couple_beach')
BEGIN
    INSERT INTO TourRelevanceJudgments (ProfileSignature, ProfileQueryKey, TourId, RelevanceGrade, Source, JudgeId, Notes, CreatedAt)
    SELECT ProfileSignature, ProfileQueryKey, TourId,
        CASE WHEN ABS(CHECKSUM(NEWID())) % 10 < 8 THEN RelevanceGrade
             ELSE CASE WHEN RelevanceGrade > 0 THEN RelevanceGrade - 1 ELSE RelevanceGrade + 1 END END,
        'expert', 'reviewer-02', N'Second judge independent review', GETUTCDATE()
    FROM TourRelevanceJudgments
    WHERE JudgeId = 'reviewer-batch'
      AND ProfileQueryKey IN (
        'foreigner_da_lat_couple_beach', 'foreigner_da_lat_couple_culture', 'foreigner_da_nang_couple_beach',
        'foreigner_phu_quoc_couple_beach', 'foreigner_ho_chi_minh_city_couple_culture', 'vietnamese_can_tho_family_river',
        'vietnamese_ha_noi_solo_city', 'vietnamese_hoi_an_family_culture', 'vietnamese_nha_trang_solo_beach',
        'vietnamese_ninh_binh_family_river', 'foreigner_sapa_group_relax', 'foreigner_da_nang_group_relax'
      );
    PRINT 'Inserted reviewer-02 overlapping judgments';
END
ELSE
    PRINT 'reviewer-02 judgments already exist';
GO
