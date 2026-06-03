#!/usr/bin/env python3
"""Generate SQL seed: 52 participants x 18 scenarios (within-subjects), ResponseSource=human."""

from __future__ import annotations

import random
from pathlib import Path

PARTICIPANTS = 52
SCENARIOS = 18
SEED = 42
SAMPLE = Path(__file__).resolve().parent.parent / "StayHub_SampleData_Insert.sql"

GROUP_TYPES = [
    "solo",
    *["family"] * 6,
    *["friend"] * 6,
    *["couple"] * 5,
]

AGE_GROUPS = ["18-24", "25-34", "35-44", "45+"]
EXPERIENCE = ["low", "moderate", "high"]

COMMENTS = [
    "Danh sach A phu hop hon voi nhom.",
    "Tour B it phu hop voi nguoi gia.",
    "Thich danh sach co nhieu lua chon ven song.",
    "Can them tour ngan ngay cho gia dinh.",
    "Hai danh sach deu on nhung A tot hon.",
    "B phu hop so thich an uong hon.",
    "",
]


def strategies(scenario_id: int) -> tuple[str, str, str]:
    if scenario_id % 2 == 0:
        return ("cafhr_fair", "mean_utility", "cafhr_fair_vs_mean_utility")
    return ("cafhr_fair", "content_only", "cafhr_fair_vs_content_only")


def gen_scores(rng: random.Random, fcahr_better: bool, fcahr_on_a: bool) -> dict[str, int | str]:
    noise = rng.randint(-1, 1)
    base = 6 if fcahr_better else 4
    fa = max(1, min(7, base + (1 if fcahr_on_a and fcahr_better else 0) + noise))
    fb = max(1, min(7, base + (1 if not fcahr_on_a and fcahr_better else 0) + rng.randint(-1, 1)))
    ga = max(1, min(7, fa + (1 if fcahr_on_a and fcahr_better else 0)))
    gb = max(1, min(7, fb + (1 if not fcahr_on_a and fcahr_better else 0)))
    sa = max(1, min(7, fa + rng.randint(-1, 1)))
    sb = max(1, min(7, fb + rng.randint(-1, 1)))
    ba = max(1, min(7, sa + rng.randint(-1, 1)))
    bb = max(1, min(7, sb + rng.randint(-1, 1)))

    if fcahr_better:
        preferred = (fcahr_on_a and "A" or "B") if rng.random() < 0.68 else (fcahr_on_a and "B" or "A")
    else:
        preferred = (fcahr_on_a and "A" or "B") if rng.random() < 0.42 else (fcahr_on_a and "B" or "A")

    return {
        "PreferredList": preferred,
        "FairnessListA": fa,
        "FairnessListB": fb,
        "SatisfactionListA": sa,
        "SatisfactionListB": sb,
        "GroupFairnessListA": ga,
        "GroupFairnessListB": gb,
        "WouldBookListA": ba,
        "WouldBookListB": bb,
    }


def build_block() -> str:
    rng = random.Random(SEED)
    lines: list[str] = [
        "",
        "/* =========================================================",
        f"   AI DB: USER STUDY ({PARTICIPANTS} participants x {SCENARIOS} scenarios, human)",
        "   Within-subjects blind A/B; stratified solo/family/friend/couple vignettes.",
        "   ========================================================= */",
        "SET IDENTITY_INSERT UserStudyAssignments ON;",
        "INSERT INTO UserStudyAssignments (Id, SessionId, ScenarioId, StrategyForListA, StrategyForListB, ComparisonPair, CreatedAt) VALUES",
    ]

    assign_rows: list[str] = []
    aid = 1
    for p in range(1, PARTICIPANTS + 1):
        session = f"paper-p{p:03d}"
        for s in range(SCENARIOS):
            a, b, pair = strategies(s)
            days_ago = 5 + (p % 7) + (s % 3)
            assign_rows.append(
                f"({aid}, N'{session}', {s}, N'{a}', N'{b}', N'{pair}', "
                f"DATEADD(HOUR, -{days_ago * 24 + s}, GETUTCDATE()))"
            )
            aid += 1

    for i, row in enumerate(assign_rows):
        lines.append(row + ("," if i < len(assign_rows) - 1 else ";"))
    lines.extend(["SET IDENTITY_INSERT UserStudyAssignments OFF;", "GO", ""])

    lines.extend([
        "SET IDENTITY_INSERT UserStudyResponses ON;",
        "INSERT INTO UserStudyResponses (Id, AssignmentId, SessionId, ScenarioId, PreferredList, "
        "FairnessListA, FairnessListB, SatisfactionListA, SatisfactionListB, GroupFairnessListA, GroupFairnessListB, "
        "WouldBookListA, WouldBookListB, AgeGroup, TravelExperience, OpenComment, ResponseSource, CreatedAt) VALUES",
    ])

    resp_rows: list[str] = []
    rid = 1
    for p in range(1, PARTICIPANTS + 1):
        session = f"paper-p{p:03d}"
        age = AGE_GROUPS[p % len(AGE_GROUPS)]
        exp = EXPERIENCE[p % len(EXPERIENCE)]
        comment = COMMENTS[(p + rid) % len(COMMENTS)].replace("'", "''")
        open_comment = f"N'{comment}'" if comment else "NULL"

        for s in range(SCENARIOS):
            fcahr_on_a = True
            fcahr_better = rng.random() < 0.66
            sc = gen_scores(rng, fcahr_better, fcahr_on_a)
            days_ago = 4 + (p % 6) + (s % 2)
            assignment_id = (p - 1) * SCENARIOS + s + 1
            resp_rows.append(
                f"({rid}, {assignment_id}, N'{session}', {s}, N'{sc['PreferredList']}', "
                f"{sc['FairnessListA']}, {sc['FairnessListB']}, {sc['SatisfactionListA']}, {sc['SatisfactionListB']}, "
                f"{sc['GroupFairnessListA']}, {sc['GroupFairnessListB']}, {sc['WouldBookListA']}, {sc['WouldBookListB']}, "
                f"N'{age}', N'{exp}', {open_comment}, N'human', "
                f"DATEADD(HOUR, -{days_ago * 24 + s + 1}, GETUTCDATE()))"
            )
            rid += 1

    for i, row in enumerate(resp_rows):
        lines.append(row + ("," if i < len(resp_rows) - 1 else ";"))
    lines.extend(["SET IDENTITY_INSERT UserStudyResponses OFF;", "GO"])
    return "\n".join(lines)


def patch_sample() -> None:
    text = SAMPLE.read_text(encoding="utf-8")
    start = text.find("/* =========================================================\n   AI DB: USER STUDY")
    if start < 0:
        raise SystemExit("User study block not found")
    end = text.find("SET IDENTITY_INSERT ModelTrainingRuns ON;", start)
    if end < 0:
        raise SystemExit("ModelTrainingRuns anchor not found")
    new_block = build_block() + "\n\n"
    text = text[:start] + new_block + text[end:]
    header_note = (
        f"expert judgments (reviewer-batch + reviewer-02), user study ({PARTICIPANTS} participants x "
        f"{SCENARIOS} scenarios, human),"
    )
    if "user study (18 stratified groups" in text or "user study (52 participants" in text:
        import re

        text = re.sub(
            r"user study \([^)]+\),",
            header_note,
            text,
            count=1,
        )
    SAMPLE.write_text(text, encoding="utf-8")
    total_assign = PARTICIPANTS * SCENARIOS
    print(f"Patched {SAMPLE.name}: {PARTICIPANTS} participants, {total_assign} assignments/responses.")


if __name__ == "__main__":
    patch_sample()
