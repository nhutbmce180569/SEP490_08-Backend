#!/usr/bin/env python3
"""Resolve displayable Wikimedia Commons images for all tours and patch sample SQL."""

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
CACHE_FALLBACK = Path("/tmp/stayhub_tour_images_cache.json")

USER_AGENT = "StayHubSeedBot/1.0 (SEP490; educational seed data)"
MAX_URL_LEN = 500

# Optional hand-picked overrides (must return HTTP 200)
CURATED: dict[str, str] = {
    "Quang Ninh": "https://upload.wikimedia.org/wikipedia/commons/thumb/4/43/Asia_Cruise_Junk_in_Halong_bay.JPG/960px-Asia_Cruise_Junk_in_Halong_bay.JPG",
}

TOUR_ROW = re.compile(
    r"^(\(\d+), (\d+), 1, (N'(?:[^']|'')*'), (N'(?:[^']|'')*'), N'Vietnam', (N'(?:[^']|'')*'), "
    r"(N'(?:[^']|'')*'), '(https://[^']+)', "
    r"(N'(?:[^']|'')*'), (N'https?://[^']+'), ('Active'[),;])",
    re.MULTILINE,
)

FALLBACK = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/4/43/"
    "Asia_Cruise_Junk_in_Halong_bay.JPG/960px-Asia_Cruise_Junk_in_Halong_bay.JPG"
)


def fetch_commons_thumb(query: str) -> str | None:
    params = urllib.parse.urlencode(
        {
            "action": "query",
            "generator": "search",
            "gsrsearch": query,
            "gsrlimit": 1,
            "prop": "pageimages",
            "piprop": "thumbnail",
            "pithumbsize": 800,
            "format": "json",
        }
    )
    url = "https://commons.wikimedia.org/w/api.php?" + params
    req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
    try:
        with urllib.request.urlopen(req, timeout=25) as resp:
            data = json.load(resp)
    except Exception:
        return None
    for page in data.get("query", {}).get("pages", {}).values():
        src = page.get("thumbnail", {}).get("source")
        if src and len(src) <= MAX_URL_LEN:
            return src
    return None


def normalize_city(city_sql: str) -> str:
    return city_sql.replace("''", "'")


def resolve_city_image(city: str, cache: dict[str, str]) -> str:
    if city in cache:
        return cache[city]
    if city in CURATED:
        cache[city] = CURATED[city]
        return cache[city]

    queries = [
        f"{city} Vietnam landscape",
        f"{city} Vietnam",
        "Vietnam tourism landscape",
    ]
    for q in queries:
        url = fetch_commons_thumb(q)
        if url:
            cache[city] = url
            time.sleep(0.35)
            return url
        time.sleep(0.2)

    cache[city] = FALLBACK
    return FALLBACK


def load_cache() -> dict[str, str]:
    for path in (CACHE, CACHE_FALLBACK):
        if path.exists():
            return {**CURATED, **json.loads(path.read_text(encoding="utf-8"))}
    return dict(CURATED)


def save_cache(cache: dict[str, str]) -> None:
    payload = json.dumps(cache, indent=2, ensure_ascii=False)
    try:
        CACHE.parent.mkdir(parents=True, exist_ok=True)
        CACHE.write_text(payload, encoding="utf-8")
    except OSError:
        CACHE_FALLBACK.write_text(payload, encoding="utf-8")


def patch_sql(text: str, cache: dict[str, str]) -> tuple[str, int]:
    cities_needed: set[str] = set()
    for m in TOUR_ROW.finditer(text):
        cities_needed.add(normalize_city(m.group(5)))

    for city in sorted(cities_needed):
        if city not in cache:
            print(f"  fetch: {city}")
            resolve_city_image(city, cache)

    updated = 0

    def repl(m: re.Match[str]) -> str:
        nonlocal updated
        city = normalize_city(m.group(5))
        url = cache.get(city, FALLBACK)
        if m.group(7) == url:
            return m.group(0)
        updated += 1
        return (
            f"{m.group(1)}, {m.group(2)}, 1, {m.group(3)}, {m.group(4)}, N'Vietnam', {m.group(5)}, "
            f"{m.group(6)}, '{url}', {m.group(7)}, {m.group(8)}, {m.group(9)}"
        )

    return TOUR_ROW.sub(repl, text), updated


def main() -> None:
    text = SAMPLE.read_text(encoding="utf-8")
    cache = load_cache()
    print(f"Cities in cache: {len(cache)}")
    new_text, n = patch_sql(text, cache)
    save_cache(cache)
    SAMPLE.write_text(new_text, encoding="utf-8")
    print(f"Updated {n} tour image URLs in {SAMPLE}")


if __name__ == "__main__":
    main()
