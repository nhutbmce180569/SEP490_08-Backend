#!/usr/bin/env python3
"""Merge tours 113-228, expert judgments, user study into StayHub_SampleData_Insert.sql"""

from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SAMPLE = ROOT / "StayHub_SampleData_Insert.sql"
EXPERT = ROOT / "StayHub_AiDb_SeedExpertJudgments.sql"
TOURS113 = ROOT / "StayHub_Tours_113_228_Insert.sql"


def extract_tour_rows() -> list[str]:
    text = TOURS113.read_text(encoding="utf-8")
    lines = []
    for line in text.splitlines():
        s = line.strip()
        if s.startswith("(") and "stayhub-tour-" in s:
            lines.append(s.rstrip(","))
    return lines


def gen_catalog_extension() -> str:
    tour_rows = extract_tour_rows()
    if len(tour_rows) != 116:
        raise ValueError(f"Expected 116 tours, got {len(tour_rows)}")

    schedule_id = 217
    itin_id = 259
    ticket_id = 649

    sched_lines = []
    itin_lines = []
    ticket_lines = []

    for i, tour_line in enumerate(tour_rows):
        tour_id = 113 + i
        day_offset = (tour_id % 45) + 14

        sched_lines.append(
            f"({schedule_id}, {tour_id}, DATEADD(DAY, {day_offset}, CAST(GETDATE() AS date)), "
            f"DATEADD(DAY, {day_offset + 2}, CAST(GETDATE() AS date)), "
            f"N'Paper evaluation schedule for tour {tour_id}.')"
        )

        city = "Vietnam"
        if ", N'Vietnam', N'" in tour_line:
            city = tour_line.split(", N'Vietnam', N'")[1].split("'")[0]

        itin_lines.append(
            f"({itin_id}, {tour_id}, 1, N'Explore {city} highlights', "
            f"N'Guided landmarks and local experiences in {city}.', "
            f"'08:00', '17:00', N'{city}', {10.0 + (tour_id % 80) * 0.01:.3f}, "
            f"{105.0 + (tour_id % 80) * 0.01:.3f}, NULL)"
        )

        base_price = 1_800_000 + (tour_id % 12) * 350_000
        for tt, mult in [(1, 1.0), (2, 0.7), (4, 0.85)]:
            price = int(base_price * mult)
            ticket_lines.append(
                f"({ticket_id}, {schedule_id}, {tt}, {price}, 20, 0, 20, 1, N'Fare tour {tour_id} type {tt}.')"
            )
            ticket_id += 1

        schedule_id += 1
        itin_id += 1

    blocks = [
        "",
        "/* =========================================================",
        "   CATALOG: TOURS 113-228 — schedules, itineraries, tickets",
        "   ========================================================= */",
        "SET IDENTITY_INSERT TourSchedules ON;",
        "INSERT INTO TourSchedules (Id, TourId, DepartureDate, ReturnDate, Note) VALUES",
        ",\n".join(sched_lines) + ";",
        "SET IDENTITY_INSERT TourSchedules OFF;",
        "GO",
        "",
        "SET IDENTITY_INSERT TourItineraries ON;",
        "INSERT INTO TourItineraries (Id, TourId, DayNumber, Title, Description, StartDuration, EndDuration, LocationName, LocationLat, LocationLng, TourismInfoId) VALUES",
        ",\n".join(itin_lines) + ";",
        "SET IDENTITY_INSERT TourItineraries OFF;",
        "GO",
        "",
        "SET IDENTITY_INSERT TourScheduleTickets ON;",
        "INSERT INTO TourScheduleTickets (Id, ScheduleId, TicketTypeId, Price, Quantity, SoldQuantity, AvailableQuantity, IsActive, Note) VALUES",
        ",\n".join(ticket_lines) + ";",
        "SET IDENTITY_INSERT TourScheduleTickets OFF;",
        "GO",
    ]
    return "\n".join(blocks), tour_rows


def gen_expert_judgments() -> str:
    text = EXPERT.read_text(encoding="utf-8")
    lines_out: list[str] = []
    capture = False
    for line in text.splitlines():
        if line.strip().startswith("DELETE FROM TourRelevanceJudgments"):
            continue
        if line.strip().startswith("INSERT INTO TourRelevanceJudgments"):
            capture = True
            lines_out.append("/* Expert relevance judgments (~184 rows) for hybrid offline evaluation */")
            lines_out.append(line)
            continue
        if capture:
            lines_out.append(line)
            if line.strip() == "GO":
                break

    lines_out.append("")
    lines_out.append("/* Seed judgments (legacy vignettes) */")
    lines_out.append(
        "INSERT INTO TourRelevanceJudgments (ProfileSignature, ProfileQueryKey, TourId, RelevanceGrade, Source, JudgeId, Notes, CreatedAt) VALUES"
    )
    lines_out.append(
        "('seed-cantho-river', 'foreigner_can_tho_couple_river', 5, 3, 'expert', 'reviewer-01', N'Mekong floating market', GETUTCDATE()),"
    )
    lines_out.append(
        "('seed-phuquoc-beach', 'vietnamese_phu_quoc_solo_beach', 1, 3, 'expert', 'reviewer-01', N'Phu Quoc beach', GETUTCDATE()),"
    )
    lines_out.append(
        "('seed-hoian-culture', 'vietnamese_hoi_an_family_culture', 3, 3, 'expert', 'reviewer-02', N'Hoi An culture', GETUTCDATE()),"
    )
    lines_out.append(
        "('seed-dalat-nature', 'foreigner_da_lat_couple_nature', 2, 3, 'expert', 'reviewer-02', N'Da Lat nature', GETUTCDATE()),"
    )
    lines_out.append(
        "('seed-hanoi-city', 'vietnamese_hanoi_solo_city', 11, 3, 'expert', 'reviewer-03', N'Hanoi photo walk', GETUTCDATE()),"
    )
    lines_out.append(
        "('seed-halong-relax', 'vietnamese_quang_ninh_couple_relax', 4, 3, 'expert', 'reviewer-03', N'Ha Long cruise', GETUTCDATE());"
    )
    lines_out.append("GO")
    lines_out.append("")
    lines_out.append("/* Second judge overlap for Cohen's kappa (reviewer-02) */")
    lines_out.append(
        "INSERT INTO TourRelevanceJudgments (ProfileSignature, ProfileQueryKey, TourId, RelevanceGrade, Source, JudgeId, Notes, CreatedAt)"
    )
    lines_out.append("SELECT ProfileSignature, ProfileQueryKey, TourId,")
    lines_out.append(
        "    CASE WHEN ABS(CHECKSUM(NEWID())) % 10 < 8 THEN RelevanceGrade"
    )
    lines_out.append(
        "         ELSE CASE WHEN RelevanceGrade > 0 THEN RelevanceGrade - 1 ELSE RelevanceGrade + 1 END END,"
    )
    lines_out.append(
        "    'expert', 'reviewer-02', N'Second judge independent review', GETUTCDATE()"
    )
    lines_out.append("FROM TourRelevanceJudgments")
    lines_out.append("WHERE JudgeId = 'reviewer-batch'")
    lines_out.append("  AND ProfileQueryKey IN (")
    lines_out.append(
        "    'foreigner_da_lat_couple_beach', 'foreigner_da_lat_couple_culture', 'foreigner_da_nang_couple_beach',"
    )
    lines_out.append(
        "    'foreigner_phu_quoc_couple_beach', 'foreigner_ho_chi_minh_city_couple_culture', 'vietnamese_can_tho_family_river',"
    )
    lines_out.append(
        "    'vietnamese_ha_noi_solo_city', 'vietnamese_hoi_an_family_culture', 'vietnamese_nha_trang_solo_beach',"
    )
    lines_out.append(
        "    'vietnamese_ninh_binh_family_river', 'foreigner_sapa_group_relax', 'foreigner_da_nang_group_relax'"
    )
    lines_out.append("  );")
    lines_out.append("GO")
    return "\n".join(lines_out)


def gen_user_study() -> str:
    """Delegate to generate_user_study_seed.py (52 x 18 human rows)."""
    import generate_user_study_seed as gen

    return gen.build_block()


def main() -> None:
    content = SAMPLE.read_text(encoding="utf-8")
    catalog_ext, tour_rows = gen_catalog_extension()

    tour_112_end = (
        "(112, 3, 1, N'Go Cong 1D0N - Signature Experience', "
        "N'Discover Go Cong with coastal heritage and local cuisine. "
        "This package includes guided activities, local experiences, and flexible free time.', "
        "N'Vietnam', N'Go Cong', N'Go Cong Center, Tien Giang', "
        "'https://picsum.photos/seed/stayhub-tour-112/1200/800', 'Active');"
    )
    if tour_112_end not in content:
        raise SystemExit("Tour 112 anchor not found")
    content = content.replace(
        tour_112_end,
        tour_112_end[:-1] + ",\n" + ",\n".join(tour_rows) + ";",
    )

    marker = (
        "SET IDENTITY_INSERT TourSchedules OFF;\nGO\n\n\n\n"
        "/* =========================================================\n   ADDITIONAL TOUR SCHEDULE ITINERARIES"
    )
    if marker not in content:
        raise SystemExit("Schedule itinerary marker not found")
    content = content.replace(marker, catalog_ext + "\n\n" + marker.replace("\n\n\n\n", "\n\n"), 1)

    old = (
        "INSERT INTO TourRelevanceJudgments (ProfileSignature, ProfileQueryKey, TourId, RelevanceGrade, Source, JudgeId, Notes, CreatedAt) VALUES\n"
        "('seed-cantho-river'"
    )
    if old not in content:
        raise SystemExit("Judgments anchor not found")
    start = content.index(old)
    end = content.index("SET IDENTITY_INSERT ModelTrainingRuns", start)
    content = content[:start] + gen_expert_judgments() + "\n\n" + content[end:]

    content = content.replace(
        "(1, 'tour_assistant_bundle', 'Completed', 12, 18, 6, 0.92,",
        "(1, 'tour_assistant_bundle', 'Completed', 228, 123, 12, 0.92,",
    )

    if "USER STUDY (18 stratified groups" not in content:
        content = content.replace(
            "SET IDENTITY_INSERT ModelTrainingRuns ON;",
            gen_user_study() + "\n\nSET IDENTITY_INSERT ModelTrainingRuns ON;",
            1,
        )

    header = """/* =========================================================
   STAYHUB SAMPLE DATA INSERT SCRIPT
   Run this AFTER StayHub_DatabaseSchema.sql — only these 2 SQL files are required.

   Contents: all microservice seeds, 228 base tours (1-228), AI interactions,
   expert judgments (reviewer-batch + reviewer-02), 18-group user study (human),
   UNESCO tourism rows. AIAPI may augment to ~936 tours in-memory for evaluation.
   ========================================================= */

"""
    if "only these 2 SQL files" not in content:
        old_h = "/* =========================================================\n   STAYHUB SAMPLE DATA INSERT SCRIPT\n"
        idx = content.index(old_h)
        end_idx = content.index("*/", idx) + 2
        content = content[:idx] + header + content[end_idx + 1 :]

    SAMPLE.write_text(content, encoding="utf-8")
    print(f"Updated {SAMPLE} ({len(content.splitlines())} lines)")


if __name__ == "__main__":
    main()
