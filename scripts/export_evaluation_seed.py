#!/usr/bin/env python3
"""Export evaluation catalog + DB seed from StayHub_SampleData_Insert.sql."""

from __future__ import annotations

import json
import re
from datetime import UTC, datetime, timedelta
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SQL_PATH = ROOT / "StayHub_SampleData_Insert.sql"
OUT_DIR = ROOT / "StayHub" / "EvaluationRunner" / "Data"
OUT_DIR.mkdir(parents=True, exist_ok=True)

TOUR_ROW = re.compile(
    r"\((\d+),\s*(\d+),\s*1,\s*N'((?:[^']|'')*)',\s*N'((?:[^']|'')*)',\s*"
    r"N'((?:[^']|'')*)',\s*N'((?:[^']|'')*)',\s*N'((?:[^']|'')*)',\s*"
    r"'([^']*)',\s*N'((?:[^']|'')*)',\s*N'((?:[^']|'')*)',\s*'Active'\)",
    re.MULTILINE,
)

DURATION_RE = re.compile(r"(\d+)D(\d+)N|(\d+)D0N|(\d+)D(\d+)N", re.I)


def unescape_sql(s: str) -> str:
    return s.replace("''", "'")


def parse_duration(name: str) -> int:
    m = DURATION_RE.search(name)
    if not m:
        return 3
    if m.group(1) and m.group(2):
        return int(m.group(1))
    if m.group(3):
        return int(m.group(3))
    return 3


def parse_tours(sql: str) -> list[dict]:
    tours: dict[int, dict] = {}
    for m in TOUR_ROW.finditer(sql):
        tid = int(m.group(1))
        name = unescape_sql(m.group(3))
        desc = unescape_sql(m.group(4))
        country = unescape_sql(m.group(5))
        city = unescape_sql(m.group(6))
        address = unescape_sql(m.group(7))
        image = m.group(8)
        source_name = unescape_sql(m.group(9))
        source_url = unescape_sql(m.group(10))
        duration = parse_duration(name)
        base_price = 1_500_000 + (tid % 12) * 450_000
        dep = (datetime.now(UTC).date() + timedelta(days=14 + tid % 45)).isoformat()
        search_doc = " ".join(
            x for x in [name, desc, country, city, address, source_name] if x
        )
        tours[tid] = {
            "id": tid,
            "categoryId": int(m.group(2)),
            "name": name,
            "description": desc,
            "country": country,
            "city": city,
            "address": address,
            "imageUrl": image,
            "sourceName": source_name,
            "sourceUrl": source_url,
            "status": "Active",
            "averageStar": 4.2 + (tid % 8) * 0.1,
            "reviewCount": 10 + tid % 50,
            "minPrice": base_price,
            "maxPrice": base_price + 800_000,
            "nextDeparture": dep,
            "durationDays": duration,
            "searchDocument": search_doc,
            "itineraryTitles": [f"Explore {city} highlights", f"Local experience in {city}"],
            "tourismInfoIds": [min(tid, 120)],
        }
    return [tours[k] for k in sorted(tours)]


def parse_judgments(sql: str) -> list[dict]:
    rows = []
    block = re.search(
        r"INSERT INTO TourRelevanceJudgments.*?VALUES\s*(.*?)(?:;\s*GO|INSERT INTO TourRelevanceJudgments)",
        sql,
        re.DOTALL | re.IGNORECASE,
    )
    if not block:
        return rows
    pattern = re.compile(
        r"\('([^']*)',\s*'([^']*)',\s*(\d+),\s*(\d+),\s*'([^']*)',\s*'([^']*)'",
    )
    for m in pattern.finditer(block.group(1)):
        rows.append(
            {
                "profileSignature": m.group(1),
                "profileQueryKey": m.group(2),
                "tourId": int(m.group(3)),
                "relevanceGrade": int(m.group(4)),
                "source": m.group(5),
                "judgeId": m.group(6),
            }
        )
    # second batch copy
    block2 = re.search(
        r"INSERT INTO TourRelevanceJudgments.*?SELECT.*?FROM TourRelevanceJudgments",
        sql,
        re.DOTALL | re.IGNORECASE,
    )
    return rows


def parse_interactions(sql: str) -> list[dict]:
    rows = []
    pattern = re.compile(
        r"\((\d+),\s*(?:NULL|\d+),\s*(\d+),\s*'([^']*)',\s*([\d.]+),\s*'([^']*)',",
    )
    for m in pattern.finditer(sql):
        rows.append(
            {
                "customerId": None,
                "tourId": int(m.group(2)),
                "interactionType": m.group(3),
                "weight": float(m.group(4)),
                "sessionId": m.group(5),
            }
        )
    return rows


def main() -> None:
    sql = SQL_PATH.read_text(encoding="utf-8", errors="replace")
    tours = parse_tours(sql)
    if len(tours) < 200:
        raise SystemExit(f"Expected ~228 tours, got {len(tours)}")

    catalog_path = OUT_DIR / "evaluation-catalog.json"
    seed_path = OUT_DIR / "evaluation-seed.json"

    catalog_path.write_text(
        json.dumps({"tours": tours, "tourism": []}, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )

    seed = {
        "judgments": parse_judgments(sql),
        "interactions": parse_interactions(sql),
    }
    seed_path.write_text(json.dumps(seed, indent=2, ensure_ascii=False), encoding="utf-8")

    print(f"Wrote {len(tours)} tours -> {catalog_path}")
    print(f"Wrote {len(seed['judgments'])} judgments, {len(seed['interactions'])} interactions -> {seed_path}")


if __name__ == "__main__":
    main()
