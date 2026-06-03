#!/usr/bin/env python3
"""Add SourceName/SourceUrl to all tour seeds and normalize real-place provenance."""

from __future__ import annotations

import re
import sys
from pathlib import Path

from tour_provenance_config import (
    CITIES_113_228,
    CITY_FIXES,
    TOURS_1_12,
    enrich_description,
    resolve_source,
)

ROOT = Path(__file__).resolve().parent.parent
SAMPLE = ROOT / "StayHub_SampleData_Insert.sql"

INSERT_COL_OLD = (
    "(Id, CategoryId, CreatedBy, Name, Description, Country, City, Address, ImageUrl, Status)"
)
INSERT_COL_NEW = (
    "(Id, CategoryId, CreatedBy, Name, Description, Country, City, Address, ImageUrl, SourceName, SourceUrl, Status)"
)

# One tour VALUES row (before provenance columns)
ROW_RE = re.compile(
    r"^\((\d+),\s*(\d+),\s*1,\s*N'((?:[^']|'')*)',\s*N'((?:[^']|'')*)',\s*"
    r"N'Vietnam',\s*N'((?:[^']|'')*)',\s*N'((?:[^']|'')*)',\s*"
    r"'([^']+)',\s*'Active'\)\s*,?\s*;?\s*$",
    re.MULTILINE,
)

DURATIONS = ["1D0N", "2D1N", "3D2N", "4D3N", "5D4N"]
THEMES = [
    "Signature Experience",
    "Local Discovery",
    "Scenic Escape",
    "Culture and Nature",
    "Weekend Getaway",
]
CATEGORIES = [1, 2, 3, 4, 5, 6, 8, 9, 11, 12]


def sql_escape(s: str) -> str:
    return s.replace("'", "''")


def build_row_113_228(tour_id: int) -> str | None:
    idx = tour_id - 113
    if idx < 0 or idx >= len(CITIES_113_228):
        return None
    city, province, highlight = CITIES_113_228[idx]
    dur = DURATIONS[idx % len(DURATIONS)]
    theme = THEMES[idx % len(THEMES)]
    cat = CATEGORIES[idx % len(CATEGORIES)]
    name = f"{city} {dur} - {theme}"
    desc_body = f"{city}, {province}: {highlight}."
    source_name, source_url = resolve_source(city, name, desc_body)
    desc = enrich_description(desc_body, source_name, source_url)
    address = f"{city}, {province}"
    image = f"https://picsum.photos/seed/stayhub-tour-{tour_id}/1200/800"
    return (
        f"({tour_id}, {cat}, 1, N'{sql_escape(name)}', N'{sql_escape(desc)}', "
        f"N'Vietnam', N'{sql_escape(city)}', N'{sql_escape(address)}', "
        f"'{image}', N'{sql_escape(source_name)}', N'{sql_escape(source_url)}', 'Active'),"
    )


def transform_row(
    tour_id: int,
    cat: str,
    name: str,
    desc: str,
    city: str,
    address: str,
    image: str,
) -> str:
    if 113 <= tour_id <= 228:
        rebuilt = build_row_113_228(tour_id)
        if rebuilt:
            return rebuilt

    city = CITY_FIXES.get(city, city)
    if tour_id in TOURS_1_12:
        city, desc_body, source_name, source_url = TOURS_1_12[tour_id]
        desc = enrich_description(desc_body, source_name, source_url)
    else:
        source_name, source_url = resolve_source(city, name, desc)
        desc = enrich_description(desc, source_name, source_url)

    return (
        f"({tour_id}, {cat}, 1, N'{sql_escape(name)}', N'{sql_escape(desc)}', "
        f"N'Vietnam', N'{sql_escape(city)}', N'{sql_escape(address)}', "
        f"'{image}', N'{sql_escape(source_name)}', N'{sql_escape(source_url)}', 'Active'),"
    )


def patch_sql(text: str) -> str:
    text = text.replace(INSERT_COL_OLD, INSERT_COL_NEW)

    def replacer(match: re.Match[str]) -> str:
        tour_id = int(match.group(1))
        return transform_row(
            tour_id,
            match.group(2),
            match.group(3).replace("''", "'"),
            match.group(4).replace("''", "'"),
            match.group(5).replace("''", "'"),
            match.group(6).replace("''", "'"),
            match.group(7),
        )

    new_text, count = ROW_RE.subn(replacer, text)
    if count != 228:
        print(f"Warning: transformed {count} tour rows (expected 228)", file=sys.stderr)
    return new_text


def fix_tour_value_commas(text: str) -> str:
    """Ensure commas between tour tuples; terminal row ends with ); not ),;"""

    def add_comma(match: re.Match[str]) -> str:
        return match.group(1) + ","

    text = re.sub(
        r"(?m)^(\(\d+, \d+, 1,.*'Active'\))$",
        add_comma,
        text,
    )
    return re.sub(r"'Active'\),;", "'Active');", text)


def patch_header(text: str) -> str:
    old = (
        "   UNESCO tourism rows. AIAPI may augment to ~936 tours in-memory for evaluation."
    )
    new = (
        "   All 228 base tours cite VNAT or UNESCO sources (SourceName/SourceUrl). "
        "AIAPI may augment to ~936 tours in-memory for evaluation."
    )
    return text.replace(old, new) if old in text else text


def main() -> None:
    raw = SAMPLE.read_text(encoding="utf-8")
    out = patch_header(fix_tour_value_commas(patch_sql(raw)))
    SAMPLE.write_text(out, encoding="utf-8")
    print(f"Updated {SAMPLE}")


if __name__ == "__main__":
    main()
