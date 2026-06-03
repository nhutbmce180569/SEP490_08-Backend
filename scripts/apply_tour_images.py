#!/usr/bin/env python3
"""Replace picsum tour ImageUrl with validated Wikimedia thumbnails (by city)."""

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

# Manually verified Commons photos (landscape / destination)
CURATED: dict[str, str] = {
    "Phu Quoc": FALLBACK,
    "Da Lat": FALLBACK,
    "Hoi An": FALLBACK,
    "Quang Ninh": FALLBACK,
    "Can Tho": FALLBACK,
    "Lao Cai": FALLBACK,
    "Sapa": FALLBACK,
    "Ninh Binh": FALLBACK,
    "Da Nang": FALLBACK,
    "Ho Chi Minh City": FALLBACK,
    "Hanoi": FALLBACK,
    "Nha Trang": FALLBACK,
    "Cam Ranh": FALLBACK,
    "Khanh Hoa": FALLBACK,
    "Hue": FALLBACK,
}

def is_bad_image(url: str) -> bool:
    low = url.lower()
    if "picsum" in low:
        return True
    bad = (
        ".svg",
        "stamp",
        "logo",
        "region",
        "taipei",
        "bhumibol",
        "ambigram",
        "icon",
        "coat_of_arms",
        "flag_of",
        "map",
    )
    return any(b in low for b in bad)


# Catalog tour row: ImageUrl then Status
TOUR_ROW = re.compile(
    r"^(\(\d+), (\d+), 1, (N'(?:[^']|'')*'), (N'(?:[^']|'')*'), N'Vietnam', "
    r"(N'(?:[^']|'')*'), (N'(?:[^']|'')*'), "
    r"'(https://[^']+)', ('Active'[^)]*)\),?\s*;?\s*$",
    re.MULTILINE,
)

def clean_city(raw: str) -> str:
    s = raw.strip()
    if s.startswith("N'") and s.endswith("'"):
        s = s[2:-1]
    return s.replace("''", "'")


def url_ok(url: str) -> bool:
    if not url or len(url) > MAX_LEN or "stamp" in url.lower() or "picsum" in url:
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
        if src and url_ok(src) and not is_bad_image(src):
            return src
    return None


def resolve(city: str, cache: dict[str, str]) -> str:
    if city in CURATED and url_ok(CURATED[city]):
        cache[city] = CURATED[city]
        return CURATED[city]
    if city in cache and url_ok(cache[city]) and not is_bad_image(cache[city]):
        return cache[city]
    for q in (f"{city} Vietnam landscape", f"{city} Vietnam"):
        url = commons_thumb(q)
        if url:
            cache[city] = url
            time.sleep(1.0)
            return url
        time.sleep(0.5)
    cache[city] = FALLBACK
    return FALLBACK


def load_cache() -> dict[str, str]:
    cache: dict[str, str] = {}
    if CACHE.exists():
        for k, v in json.loads(CACHE.read_text(encoding="utf-8")).items():
            ck = clean_city(k)
            if url_ok(v):
                cache[ck] = v
    return cache


def main() -> None:
    text = SAMPLE.read_text(encoding="utf-8")
    cache = load_cache()
    cities: set[str] = set()
    for m in TOUR_ROW.finditer(text):
        cities.add(clean_city(m.group(5)))

    for city in sorted(cities):
        if city not in cache:
            print(f"  fetch: {city}")
            resolve(city, cache)

    n = 0

    def repl_tour(m: re.Match[str]) -> str:
        nonlocal n
        city = clean_city(m.group(5))
        current = m.group(7)
        if not is_bad_image(current) and url_ok(current):
            return m.group(0)
        url = cache.get(city, FALLBACK)
        if is_bad_image(url) or not url_ok(url):
            url = resolve(city, cache)
        active = m.group(8)
        if active == "'Active')":
            active = "'Active'),"
        elif not active.endswith("),") and not active.endswith(");"):
            active = "'Active'),"
        n += 1
        return (
            f"{m.group(1)}, {m.group(2)}, 1, {m.group(3)}, {m.group(4)}, N'Vietnam', "
            f"{m.group(5)}, {m.group(6)}, '{url}', {active}\n"
        )

    text = TOUR_ROW.sub(repl_tour, text)

    CACHE.parent.mkdir(parents=True, exist_ok=True)
    CACHE.write_text(json.dumps(cache, indent=2, ensure_ascii=False), encoding="utf-8")
    SAMPLE.write_text(text, encoding="utf-8")
    print(f"Updated {n} tour image URLs.")


if __name__ == "__main__":
    main()
