#!/usr/bin/env python3
"""One-shot fix: repair tour INSERT rows and set Wikimedia ImageUrl per city."""

from __future__ import annotations

import json
import re
from pathlib import Path

from tour_images_curated import resolve_image

SAMPLE = Path(__file__).resolve().parent.parent / "StayHub_SampleData_Insert.sql"
CACHE = Path(__file__).resolve().parent / "data" / "tour_images_cache.json"

PROV = re.compile(
    r"^\((?P<id>\d+), (?P<cat>\d+), 1, "
    r"(?P<name>N'(?:[^']|'')*'), (?P<desc>N'(?:[^']|'')*'), N'Vietnam', "
    r"(?P<city>N'(?:[^']|'')*'), (?P<addr>N'(?:[^']|'')*'), "
    r"'(?P<img>https://[^']+)', "
    r"(?:https://picsum\.photos/seed/stayhub-tour(?:-old)?-\d+/1200/800, )?"
    r"(?P<srcname>N'(?:[^']|'')*'), (?P<srcurl>N'https?://[^']+'),?\s*"
    r"(?P<active>'Active'[^)]*)?,?\s*;?\s*$"
)

LEG = re.compile(
    r"^\((?P<id>\d+), (?P<cat>\d+), 1, "
    r"(?P<name>N'(?:[^']|'')*'), (?P<desc>N'(?:[^']|'')*'), N'Vietnam', "
    r"(?P<city>N'(?:[^']|'')*'), (?P<addr>N'(?:[^']|'')*'), "
    r"'https://picsum\.photos/seed/stayhub-tour(?:-old)?-\d+/1200/800', "
    r"('Active'[^)]*)\),?\s*;?\s*$"
)


def clean_city(raw: str) -> str:
    s = raw.strip()
    if s.startswith("N'") and s.endswith("'"):
        s = s[2:-1]
    return s.replace("''", "'")


def resolve(city: str, addr: str, cache: dict[str, str]) -> str:
    url = resolve_image(city, addr)
    if len(url) > 500:
        url = url[:500]
    cache[city] = url
    return url


def fmt_prov(m: re.Match[str], img: str) -> str:
    tid = int(m.group("id"))
    act = m.group("active") or ""
    if "'Active'" not in act:
        act = "'Active');" if tid in (12, 92, 228) else "'Active'),"
    elif act == "'Active')," and tid in (12, 92, 228):
        act = "'Active');"
    return (
        f"({tid}, {m.group('cat')}, 1, {m.group('name')}, {m.group('desc')}, N'Vietnam', "
        f"{m.group('city')}, {m.group('addr')}, '{img}', "
        f"{m.group('srcname')}, {m.group('srcurl')}, {act}\n"
    )


def fmt_leg(m: re.Match[str], img: str) -> str:
    tid = int(m.group("id"))
    act = m.group(1)
    if act == "'Active')," and tid in (12, 92, 228):
        act = "'Active');"
    return (
        f"({tid}, {m.group('cat')}, 1, {m.group('name')}, {m.group('desc')}, N'Vietnam', "
        f"{m.group('city')}, {m.group('addr')}, '{img}', {act}\n"
    )


def main() -> None:
    cache: dict[str, str] = {}

    lines = SAMPLE.read_text(encoding="utf-8").splitlines(keepends=True)
    out, n = [], 0
    for line in lines:
        s = line.rstrip("\n")
        m = PROV.match(s) or LEG.match(s)
        if not m:
            out.append(line)
            continue
        city = clean_city(m.group("city"))
        addr = m.group("addr")
        img = resolve(city, addr, cache)
        new = fmt_prov(m, img) if m.re is PROV else fmt_leg(m, img)
        if new.rstrip() != s:
            n += 1
        out.append(new)

    CACHE.parent.mkdir(parents=True, exist_ok=True)
    CACHE.write_text(json.dumps(cache, indent=2, ensure_ascii=False), encoding="utf-8")
    SAMPLE.write_text("".join(out), encoding="utf-8")
    print(f"Fixed {n} tour rows. Cities in cache: {len(cache)}.")


if __name__ == "__main__":
    main()
