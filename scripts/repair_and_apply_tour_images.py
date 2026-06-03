#!/usr/bin/env python3
"""Fix corrupted tour INSERT rows and apply per-city Wikimedia images."""

from __future__ import annotations

import json
import re
import time
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
SAMPLE = ROOT / "StayHub_SampleData_Insert.sql"
CACHE = Path(__file__).resolve().parent / "data" / "tour_images_cache.json"

UA = "StayHubSeedBot/1.0 (SEP490; educational)"
MAX_LEN = 500
FALLBACK = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/4/43/"
    "Asia_Cruise_Junk_in_Halong_bay.JPG/960px-Asia_Cruise_Junk_in_Halong_bay.JPG"
)

TOUR = re.compile(
    r"^\((?P<id>\d+), (?P<cat>\d+), 1, "
    r"(?P<name>N'(?:[^']|'')*'), (?P<desc>N'(?:[^']|'')*'), N'Vietnam', "
    r"(?P<city>N'(?:[^']|'')*'), (?P<addr>N'(?:[^']|'')*'), "
    r"'(?P<img>https://[^']+)', "
    r"(?:https://picsum\.photos/seed/stayhub-tour(?:-old)?-\d+/1200/800, )?"
    r"(?P<srcname>N'(?:[^']|'')*'), (?P<srcurl>N'https?://[^']+'),?\s*"
    r"(?P<active>'Active'[^)]*)?,?\s*;?\s*$"
)


def clean_city(raw: str) -> str:
    s = raw.strip()
    if s.startswith("N'") and s.endswith("'"):
        s = s[2:-1]
    return s.replace("''", "'")


def url_ok(url: str) -> bool:
    if not url or len(url) > MAX_LEN:
        return False
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    try:
        with urllib.request.urlopen(req, timeout=20) as r:
            return 200 <= r.status < 400
    except Exception:
        return False


def commons_thumb(query: str) -> str | None:
    params = urllib.parse.urlencode(
        {
            "action": "query",
            "generator": "search",
            "gsrsearch": query,
            "gsrlimit": 5,
            "prop": "pageimages",
            "piprop": "thumbnail",
            "pithumbsize": 800,
            "format": "json",
        }
    )
    req = urllib.request.Request(
        "https://commons.wikimedia.org/w/api.php?" + params,
        headers={"User-Agent": UA},
    )
    try:
        with urllib.request.urlopen(req, timeout=25) as resp:
            data = json.load(resp)
    except Exception:
        return None
    for page in sorted(
        data.get("query", {}).get("pages", {}).values(),
        key=lambda p: p.get("index", 0),
    ):
        title = (page.get("title") or "").lower()
        if any(x in title for x in ("stamp", "map", "logo", "flag", "coat", "diagram", "icon")):
            continue
        src = page.get("thumbnail", {}).get("source")
        if src and url_ok(src):
            return src
    return None


def resolve(city: str, cache: dict[str, str]) -> str:
    if city in cache and url_ok(cache[city]):
        return cache[city]
    for q in (f"{city} Vietnam landscape", f"{city} Vietnam"):
        url = commons_thumb(q)
        if url:
            cache[city] = url
            time.sleep(1.1)
            return url
        time.sleep(0.6)
    cache[city] = FALLBACK
    return FALLBACK


def format_row(m: re.Match[str], image: str) -> str:
    tid = int(m.group("id"))
    active = m.group("active") or ""
    if "'Active'" not in active:
        active = "'Active');" if tid == 12 or tid == 228 or tid == 92 else "'Active'),"
    elif active == "'Active')," and tid in (12, 228, 92):
        active = "'Active');"
    return (
        f"({tid}, {m.group('cat')}, 1, {m.group('name')}, {m.group('desc')}, N'Vietnam', "
        f"{m.group('city')}, {m.group('addr')}, '{image}', "
        f"{m.group('srcname')}, {m.group('srcurl')}, {active}\n"
    )


def main() -> None:
    cache: dict[str, str] = {}
    if CACHE.exists():
        raw = json.loads(CACHE.read_text(encoding="utf-8"))
        cache = {clean_city(k): v for k, v in raw.items() if url_ok(v)}

    text = SAMPLE.read_text(encoding="utf-8")
    cities: set[str] = set()
    matches = list(TOUR.finditer(text))
    for m in matches:
        cities.add(clean_city(m.group("city")))

    for city in sorted(cities):
        if city not in cache:
            print(f"  resolve: {city}")
            resolve(city, cache)

    n = 0

    def repl(m: re.Match[str]) -> str:
        nonlocal n
        city = clean_city(m.group("city"))
        img = cache.get(city, FALLBACK)
        row = format_row(m, img)
        if row != m.group(0) + ("\n" if not m.group(0).endswith("\n") else ""):
            n += 1
        return row

    new_text = TOUR.sub(repl, text)
    CACHE.parent.mkdir(parents=True, exist_ok=True)
    CACHE.write_text(json.dumps(cache, indent=2, ensure_ascii=False), encoding="utf-8")
    SAMPLE.write_text(new_text, encoding="utf-8")
    print(f"Matched {len(matches)} tour rows, rebuilt {n}.")


if __name__ == "__main__":
    main()
