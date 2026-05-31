#!/usr/bin/env python3
"""Generate StayHub_AiDb_SeedExpertJudgments.sql for 40 synthetic eval profile keys."""

tours = {
    1: ("Phu Quoc", "beach relax"),
    2: ("Da Lat", "culture nature relax"),
    3: ("Hoi An", "culture river"),
    4: ("Quang Ninh", "relax cruise"),
    5: ("Can Tho", "river culture food"),
    6: ("Sapa", "nature adventure trek"),
    7: ("Ninh Binh", "culture nature river"),
    8: ("Da Nang", "beach city culture"),
    9: ("Ho Chi Minh City", "city food"),
    10: ("Cam Ranh", "beach relax honeymoon"),
    11: ("Hanoi", "city photo culture"),
    12: ("Nha Trang", "beach dive adventure"),
}

city_tour = {
    "can_tho": 5,
    "da_lat": 2,
    "hoi_an": 3,
    "phu_quoc": 1,
    "ha_noi": 11,
    "da_nang": 8,
    "nha_trang": 12,
    "sapa": 6,
    "ninh_binh": 7,
    "ho_chi_minh_city": 9,
}

interest_boost = {
    "beach": [1, 10, 12],
    "relax": [1, 4, 10],
    "culture": [3, 7, 11],
    "food": [9, 3, 5],
    "river": [5, 7, 3],
    "nature": [6, 2, 7],
    "adventure": [6, 12, 7],
    "city": [9, 11, 8],
    "photography": [11, 8, 2],
}

cities = [
    "Can Tho", "Da Lat", "Hoi An", "Phu Quoc", "Ha Noi",
    "Da Nang", "Nha Trang", "Sapa", "Ninh Binh", "Ho Chi Minh City",
]
interest_sets = [
    ["beach", "relax"], ["culture", "food"], ["river", "food", "culture"],
    ["nature", "adventure"], ["city", "food", "photography"], ["beach", "photography"],
    ["culture", "river"], ["relax", "photography"],
]
companions = ["Solo", "Couple", "Family", "Group"]
nationalities = ["Vietnamese", "Foreigner"]


def query_key(i: int) -> str:
    city = cities[i % 10].lower().replace(" ", "_")
    return f"{nationalities[i % 2].lower()}_{city}_{companions[i % 4].lower()}_{interest_sets[i % 8][0]}"


def grades_for_key(key: str) -> list[tuple[str, int, int, str]]:
    city = next(c for c in city_tour if f"_{c}_" in f"_{key}_")
    top_interest = key.split("_")[-1]
    primary = city_tour[city]
    rows: list[tuple[int, int, str]] = [(primary, 3, f"Primary tour in {city.replace('_', ' ')}")]

    for tid in interest_boost.get(top_interest, []):
        if tid == primary:
            continue
        tour_city = tours[tid][0].lower().replace(" ", "_")
        if top_interest in tours[tid][1]:
            grade = 2
        elif tour_city in city:
            grade = 2
        else:
            grade = 0
        if tid == 6 and top_interest in ("beach", "relax") and "family" in key:
            grade = 0
        rows.append((tid, grade, f"Secondary fit for {top_interest}"))

    neg = 6 if city != "sapa" else 9
    if not any(r[0] == neg for r in rows):
        rows.append((neg, 0, "Irrelevant destination"))

    merged: dict[int, tuple[int, str]] = {}
    for tid, grade, note in rows:
        if tid not in merged or grade > merged[tid][0]:
            merged[tid] = (grade, note)

    return [(key, tid, merged[tid][0], merged[tid][1]) for tid in sorted(merged)]


def main() -> None:
    keys = sorted({query_key(i) for i in range(100)})
    all_rows: list[tuple[str, int, int, str]] = []
    for k in keys:
        all_rows.extend(grades_for_key(k))

    out_path = "/home/kiuthi/Projects/Backend/SEP490_08-Backend/StayHub_AiDb_SeedExpertJudgments.sql"
    lines = [
        "-- Expanded expert judgments for 40 synthetic eval profile keys (seed=42, n=100)",
        "-- Run after StayHub_AiDb_Patch.sql and StayHub_AiDb_SeedEval.sql",
        "USE StayHub_AiDb;",
        "GO",
        "",
        "DELETE FROM TourRelevanceJudgments WHERE JudgeId IN ('reviewer-batch', 'reviewer-01', 'reviewer-02', 'reviewer-03');",
        "GO",
        "",
        f"-- {len(all_rows)} judgments across {len(keys)} profile query keys",
        "INSERT INTO TourRelevanceJudgments (ProfileSignature, ProfileQueryKey, TourId, RelevanceGrade, Source, JudgeId, Notes, CreatedAt) VALUES",
    ]

    values = []
    for qk, tid, grade, note in all_rows:
        sig = f"exp-{qk[:24]}"
        note_esc = note.replace("'", "''")
        values.append(
            f"('{sig}', '{qk}', {tid}, {grade}, 'expert', 'reviewer-batch', N'{note_esc}', GETUTCDATE())"
        )

    lines.append(",\n".join(values) + ";")
    lines.extend(["GO", "", "PRINT 'Expert judgments batch import completed.';", "GO"])

    with open(out_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print(f"Wrote {len(all_rows)} judgments to {out_path}")


if __name__ == "__main__":
    main()
