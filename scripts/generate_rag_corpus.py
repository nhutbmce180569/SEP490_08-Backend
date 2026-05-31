#!/usr/bin/env python3
"""Generate StayHub Vietnam RAG corpus for conference-grade knowledge augmentation."""

import json
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "StayHub/AIAPI/Data/vietnam-rag-corpus.json"

# tourId mapping from StayHub catalog
TOURS = {
    "phu_quoc": 1, "da_lat": 2, "hoi_an": 3, "ha_long": 4, "can_tho": 5,
    "sapa": 6, "ninh_binh": 7, "da_nang": 8, "ho_chi_minh": 9, "cam_ranh": 10,
    "hanoi": 11, "nha_trang": 12,
}

UNESCO = {
    "hoi_an": ("https://whc.unesco.org/en/list/874", "Hoi An Ancient Town"),
    "ha_long": ("https://whc.unesco.org/en/list/672", "Ha Long Bay"),
    "hue": ("https://whc.unesco.org/en/list/678", "Complex of Hue Monuments"),
    "hanoi": ("https://whc.unesco.org/en/list/1328", "Central Sector of Thang Long Imperial Citadel"),
    "ninh_binh": ("https://whc.unesco.org/en/list/1438", "Trang An Landscape Complex"),
    "phong_nha": ("https://whc.unesco.org/en/list/951", "Phong Nha-Ke Bang National Park"),
    "my_son": ("https://whc.unesco.org/en/list/949", "My Son Sanctuary"),
    "citadel_hue": ("https://whc.unesco.org/en/list/678", "Complex of Hue Monuments"),
}

VNAT = ("Vietnam National Administration of Tourism", "https://vietnamtourism.gov.vn/en", "National")
UNESCO_VN = ("UNESCO World Heritage Centre - Vietnam", "https://whc.unesco.org/en/statesparties/vn", "International")


def src(name, url, authority="International"):
    return {"name": name, "url": url, "authority": authority}


def chunk(cid, title, content, city_keys, region, interests, personas, tour_ids,
          chunk_type, heritage=None, source=None):
    return {
        "id": cid,
        "title": title,
        "content": content,
        "cityKeys": city_keys,
        "region": region,
        "interestTags": interests,
        "personaTags": personas,
        "relatedTourIds": tour_ids,
        "heritageLevel": heritage or "Regional",
        "chunkType": chunk_type,
        "source": source or src(*VNAT)
    }


def main():
    chunks = []
    n = 0

    def add(**kw):
        nonlocal n
        n += 1
        kw.setdefault("cid", f"rag-{n:03d}")
        chunks.append(chunk(**kw))

    # === UNESCO heritage deep dives ===
    add(title="Hoi An Ancient Town UNESCO heritage significance",
        content="Hoi An Ancient Town (UNESCO 1999) preserves a trading port from the 15th-19th centuries with "
                "Japanese Bridge, merchant houses, and assembly halls. Lantern festivals and pedestrian evenings "
                "support slow cultural tourism. Best for culture, photography, and food-focused itineraries.",
        city_keys=["hoian", "hoi an"], region="Central Vietnam",
        interests=["culture", "photography", "food"], personas=["foreigner", "couple", "family"],
        tour_ids=[3, 8], chunk_type="heritage", heritage="UNESCO World Heritage",
        source=src("UNESCO - Hoi An", UNESCO["hoi_an"][0]))

    add(title="Ha Long Bay karst seascape and cruise tourism",
        content="Ha Long Bay UNESCO site features ~1,600 limestone islands. Overnight luxury cruises combine "
                "cave visits (Sung Sot), kayaking, and onboard dining. Elderly travelers should select cruises "
                "with elevator cabins; families need life jackets for kayak segments.",
        city_keys=["halong", "quangninh", "ha long"], region="Northeast Vietnam",
        interests=["relax", "nature", "photography"], personas=["couple", "family", "foreigner"],
        tour_ids=[4], chunk_type="heritage", heritage="UNESCO World Heritage",
        source=src("UNESCO - Ha Long Bay", UNESCO["ha_long"][0]))

    add(title="Trang An Ninh Binh mixed natural-cultural landscape",
        content="Trang An Landscape Complex (UNESCO 2014) offers boat routes through limestone caves and temples. "
                "Seated boat tours suit elderly and children. Combine with Mua Cave viewpoint and Hoa Lu ancient capital.",
        city_keys=["ninhbinh", "trangan", "ninh binh"], region="Red River Delta",
        interests=["culture", "nature", "river"], personas=["family", "elderly", "foreigner"],
        tour_ids=[7], chunk_type="heritage", heritage="UNESCO World Heritage",
        source=src("UNESCO - Trang An", UNESCO["ninh_binh"][0]))

    add(title="Thang Long Imperial Citadel Hanoi",
        content="Central Sector of Thang Long Imperial Citadel (UNESCO 2010) documents Vietnamese dynastic history. "
                "Pair with Old Quarter street food walks, Hoan Kiem Lake, and water puppet theatre for city tours.",
        city_keys=["hanoi", "ha noi"], region="Red River Delta",
        interests=["culture", "city", "food"], personas=["foreigner", "solo"],
        tour_ids=[11], chunk_type="heritage", heritage="UNESCO World Heritage",
        source=src("UNESCO - Thang Long", UNESCO["hanoi"][0]))

    add(title="Complex of Hue Monuments imperial heritage",
        content="Hue imperial citadel and royal tombs form a UNESCO ensemble reflecting Nguyen dynasty architecture. "
                "Perfume River boat trips and royal cuisine tasting are signature experiences. Electric carts "
                "recommended inside the citadel for elderly visitors.",
        city_keys=["hue"], region="Central Vietnam",
        interests=["culture", "river", "food"], personas=["foreigner", "couple", "elderly"],
        tour_ids=[], chunk_type="heritage", heritage="UNESCO World Heritage",
        source=src("UNESCO - Hue", UNESCO["hue"][0]))

    add(title="My Son Cham sanctuary near Hoi An",
        content="My Son Sanctuary (UNESCO 1999) preserves red-brick Cham tower temples from the 4th-13th centuries. "
                "Day trips from Hoi An or Da Nang suit culture-focused travelers. Morning visits avoid heat.",
        city_keys=["myson", "hoian", "da nang"], region="Central Vietnam",
        interests=["culture", "photography"], personas=["foreigner", "couple"],
        tour_ids=[3, 8], chunk_type="heritage", heritage="UNESCO World Heritage",
        source=src("UNESCO - My Son", UNESCO["my_son"][0]))

    # === Destination packs (heritage + food + accessibility + season) ===
    destinations = [
        ("can_tho", "Can Tho / Mekong Delta", "Mekong Delta", [5],
         "Cai Rang floating market operates at dawn; river tourism needs sun protection and stable footwear.",
         "Mekong cuisine: Banh Xeo, freshwater fish, coconut candy. Cash preferred at floating vendors.",
         "Morning boat departures suit elderly; avoid long open-deck midday segments with children.",
         "Dry season Dec-May lowers flood risk; floating markets busiest before 8 AM."),
        ("phu_quoc", "Phu Quoc Island", "Southwest Coast", [1, 10],
         "Phu Quoc offers marine parks, Sao Beach white sand, and night market seafood.",
         "Phu Quoc fish sauce and pepper farms support food tourism; try herring salad (goi ca mai).",
         "Resort-based tours reduce mobility strain for elderly; shallow beaches suit families.",
         "Dry season Nov-Apr preferred for beach; monsoon Jun-Oct may restrict boat trips."),
        ("da_lat", "Da Lat Central Highlands", "Central Highlands", [2],
         "Da Lat temperate climate supports colonial villas, flower gardens, and specialty coffee.",
         "Night market street food and avocado ice cream are local highlights.",
         "Cloud-hunting requires pre-dawn departures — not ideal for very young children.",
         "Year-round cool evenings; bring light jacket even in summer."),
        ("sapa", "Sapa / Lao Cai", "Northwest Vietnam", [6],
         "Sapa terraced fields and ethnic minority villages (Hmong, Dao) define cultural trekking.",
         "Local cuisine includes thang co and grilled mountain pork; homestays offer authentic meals.",
         "Long treks unsuitable for elderly and toddlers; Fansipan cable car is lower-impact alternative.",
         "Sep-Nov and Mar-May offer clearer views; winter fog common Dec-Feb."),
        ("da_nang", "Da Nang Central Coast", "Central Coast", [8],
         "Da Nang links Dragon Bridge, My Khe Beach, and gateway to Ba Na Hills Golden Bridge.",
         "Mi Quang noodles and seafood boulevard are culinary anchors.",
         "Ba Na Hills cable car aids elderly access to hill attractions.",
         "Beach safety flags critical during Sep-Dec monsoon swells."),
        ("nha_trang", "Nha Trang / Khanh Hoa", "South Central Coast", [12, 10],
         "Nha Trang bay supports island hopping, diving, and Ponagar Cham towers.",
         "Banh Can mini pancakes and fresh seafood define local food identity.",
         "Licensed dive operators required; island day trips with seated transport suit elderly.",
         "Best sea conditions Feb-Aug; typhoon season Oct-Dec may cancel boats."),
        ("ho_chi_minh", "Ho Chi Minh City", "Southeast Vietnam", [9],
         "Saigon blends colonial Notre-Dame area, Ben Thanh Market, and Cu Chi tunnel day trips.",
         "Street food tours (Banh Mi, Hu Tieu) are signature urban experiences.",
         "Air-conditioned vehicle tours reduce heat stress for elderly in dry season.",
         "Hot dry season Mar-May; rainy afternoons May-Nov — plan indoor museums."),
    ]

    types = [
        ("heritage", 0, ["culture"], ["foreigner"]),
        ("food", 1, ["food"], ["couple", "family"]),
        ("accessibility", 2, ["relax"], ["elderly", "children"]),
        ("season", 3, ["nature"], ["solo", "group"]),
    ]

    for city, name, region, tids, *texts in destinations:
        for ctype, idx, ints, pers in types:
            add(title=f"{name} — {ctype}",
                content=texts[idx],
                city_keys=[city.replace("_", ""), city.replace("_", " ")],
                region=region, interests=ints, personas=pers,
                tour_ids=tids, chunk_type=ctype)

    # === Interest-topic cross-cutting chunks ===
    interest_topics = [
        ("beach", "Vietnam coastal beach tourism patterns",
         "Key beach clusters: Phu Quoc, Nha Trang, Da Nang, Mui Ne, Phu Quy. Resort packages suit couples; "
         "check jellyfish season and lifeguard presence for family beach days."),
        ("river", "Mekong and Red River delta river tourism",
         "River tourism spans Cai Rang floating market, Mekong homestays, Perfume River Hue, and Trang An boats. "
         "Life jackets and licensed operators are mandatory safety requirements."),
        ("culture", "Vietnamese intangible cultural heritage for tourists",
         "Water puppetry (Hanoi), Quan Ho folk songs (Bac Ninh), Ca Tru, and lantern festivals (Hoi An) "
         "provide accessible cultural entry points for international visitors."),
        ("food", "Vietnamese regional cuisine map for tour planning",
         "North: Pho, Bun Cha. Central: Mi Quang, Cao Lau. South: Hu Tieu, Banh Xeo. "
         "Street food tours need reputable operators for hygiene-conscious foreign guests."),
        ("nature", "Vietnam nature and national park tourism",
         "National parks include Cat Ba, Cuc Phuong, Phong Nha-Ke Bang, Bach Ma. Trek difficulty varies; "
         "elderly should prefer cable car or boat-based nature experiences."),
        ("adventure", "Adventure tourism safety in Vietnam",
         "Trekking (Sapa), diving (Nha Trang), kayaking (Ha Long), canyoning (Da Lat) require operator licensing. "
         "Not recommended for groups with young children or mobility-limited elderly."),
        ("relax", "Resort and wellness tourism in Vietnam",
         "Cam Ranh and Phu Quoc lead resort/spa offerings; Ha Long luxury cruises combine relaxation with scenery. "
         "Honeymoon packages emphasize privacy and spa access."),
        ("photography", "Photography tourism hotspots Vietnam",
         "Golden hour spots: Hoi An lanterns, Ha Long sunrise decks, Sapa terraces (Sep harvest), "
         "Da Lat cloud hunting (Cau Dat 4:30 AM departures)."),
        ("city", "Urban tourism in Vietnam major cities",
         "Hanoi and Ho Chi Minh City offer museums, markets, and colonial architecture. "
         "Traffic density requires guided transport for first-time foreign visitors."),
    ]

    for i, (tag, title, content) in enumerate(interest_topics):
        add(title=title, content=content,
            city_keys=["vietnam"], region="National",
            interests=[tag], personas=["foreigner", "vietnamese", "solo", "couple"],
            tour_ids=list(TOURS.values()), chunk_type="topic", heritage="National")

    # === Persona-specific etiquette ===
    add(title="Foreign visitor etiquette and visa basics",
        content="Dress modestly at temples; remove shoes; ask before photographing people. "
                "E-visa available for many nationalities; Phu Quoc special zone policies may differ. "
                "Tipping not mandatory but appreciated for guides.",
        city_keys=["vietnam"], region="National",
        interests=["culture", "city"], personas=["foreigner"],
        tour_ids=[], chunk_type="etiquette", heritage="National",
        source=src(*VNAT))

    add(title="Elderly-friendly tour design principles Vietnam",
        content="Prefer morning schedules, seated transport, elevator-equipped hotels/cruises, "
                "minimal stair climbing. Avoid motorbike street food tours and multi-hour treks. "
                "Ha Long cruises and Trang An boats offer seated experiences.",
        city_keys=["vietnam"], region="National",
        interests=["relax", "culture"], personas=["elderly"],
        tour_ids=[4, 5, 7, 11], chunk_type="accessibility", heritage="National")

    add(title="Family travel with children in Vietnam",
        content="Theme parks (Ba Na Hills), shallow beaches (Phu Quoc), water puppet shows (Hanoi), "
                "and lantern workshops (Hoi An) suit mixed-age groups. Limit dive/trek/motorbike activities.",
        city_keys=["vietnam"], region="National",
        interests=["beach", "culture", "relax"], personas=["children", "family"],
        tour_ids=[1, 2, 3, 8], chunk_type="accessibility", heritage="National")

    add(title="Group dynamics and fair activity selection",
        content="Group tour fairness requires balancing adventure seekers with comfort-preferring members. "
                "Itineraries with optional activities (kayak vs deck rest) reduce dissatisfaction variance. "
                "River and resort bases offer natural split activity windows.",
        city_keys=["vietnam"], region="National",
        interests=["adventure", "relax"], personas=["group", "couple"],
        tour_ids=[4, 5, 8], chunk_type="group_fairness", heritage="National")

    add(title="Vietnam monsoon and typhoon travel advisories",
        content="Central coast typhoon risk peaks Sep-Dec affecting Da Nang, Hoi An, Nha Trang boats. "
                "Mekong Delta flooding possible Jun-Nov. North winter fog affects Sapa views Dec-Feb.",
        city_keys=["vietnam", "da nang", "nha trang", "can tho", "sapa"],
        region="National", interests=["nature", "beach"], personas=["foreigner", "family"],
        tour_ids=[4, 6, 8, 12], chunk_type="season", heritage="National",
        source=src("General Statistics Office Vietnam - Climate", "https://www.gso.gov.vn", "National"))

    # === Tour-package aligned chunks (StayHub catalog tourId 1-12) ===
    tour_packages = [
        (1, "Phu Quoc 4D3N resort package", "phu_quoc",
         "Resort stay on Phu Quoc with Sao Beach, Sunset Town, and night market. Ideal beach-relax couples and solo.",
         ["beach", "relax", "photography"], ["solo", "couple"]),
        (2, "Da Lat 3D2N cloud hunting", "da_lat",
         "Youthful Da Lat itinerary: Cau Dat cloud hunting, coffee farms, night market. Culture-nature blend.",
         ["nature", "photography", "food"], ["couple", "group"]),
        (3, "Hoi An 2D1N ancient town lanterns", "hoi_an",
         "Ancient town walking, Cao Lau tasting, flower lantern release. UNESCO core experience.",
         ["culture", "food", "photography"], ["couple", "family"]),
        (4, "Ha Long 2D1N luxury cruise", "ha_long",
         "Overnight bay cruise with cave visits and gala dinner. Premium relax-nature product.",
         ["relax", "nature", "photography"], ["couple", "family"]),
        (5, "Mekong Delta 2D1N floating market", "can_tho",
         "Cai Rang floating market, fruit orchards, homestay cuisine. River-food-cultural triangle.",
         ["river", "food", "culture"], ["family", "foreigner"]),
        (6, "Sapa 4D3N village trekking", "sapa",
         "Cat Cat village, terraced fields, Fansipan option. Adventure-nature; limited elderly fit.",
         ["nature", "adventure", "culture"], ["group", "solo"]),
        (7, "Ninh Binh 2D1N Trang An Mua Cave", "ninh_binh",
         "Trang An boat, Mua Cave viewpoint, Hoa Lu capital. Accessible nature-culture day trips.",
         ["culture", "nature", "river"], ["family", "elderly"]),
        (8, "Da Nang 3D2N Dragon Bridge Ba Na", "da_nang",
         "Coastal city, Dragon Bridge show, Ba Na Hills Golden Bridge. Mixed beach-city-theme.",
         ["beach", "city", "photography"], ["couple", "family"]),
        (9, "Saigon evening food tour", "ho_chi_minh",
         "Motorbike or walking street food: Banh Mi, Hu Tieu, district nightlife. Urban food focus.",
         ["food", "city", "culture"], ["solo", "couple"]),
        (10, "Cam Ranh honeymoon 3D2N", "cam_ranh",
         "Private beach resort, spa, romantic dinner. Couple-relax premium positioning.",
         ["relax", "beach"], ["couple"]),
        (11, "Hanoi photo walk 1 day", "hanoi",
         "Old Quarter, Hoan Kiem, Bun Cha, egg coffee photo hunting. Short city-photography product.",
         ["city", "photography", "food"], ["solo", "couple"]),
        (12, "Nha Trang island hopping dive", "nha_trang",
         "Scuba, Binh Ba Island, Banh Can. Adventure-beach; check child age for diving.",
         ["beach", "adventure", "food"], ["group", "couple"]),
    ]

    for tid, title, city, content, ints, pers in tour_packages:
        add(title=title, content=content,
            city_keys=[city.replace("_", ""), city.replace("_", " ")],
            region="Tour Package", interests=ints, personas=pers,
            tour_ids=[tid], chunk_type="tour_package", heritage="Platform-Aligned")

    # === Regional food micro-chunks ===
    foods = [
        ("cao_lau", "Hoi An Cao Lau noodles", "hoian", [3], "Signature chewy noodles with char siu — UNESCO town food icon."),
        ("banh_xeo", "Mekong Banh Xeo", "cantho", [5], "Crispy turmeric crepe with herbs — delta street food staple."),
        ("bun_cha", "Hanoi Bun Cha", "hanoi", [11], "Grilled pork with noodles — Obama-era global fame dish."),
        ("banh_can", "Nha Trang Banh Can", "nhatrang", [12], "Mini clay-oven cakes with dipping sauce."),
        ("mi_quang", "Da Nang Mi Quang", "danang", [8], "Turmeric noodles with minimal broth — central Vietnam icon."),
        ("banh_mi", "Saigon Banh Mi", "hochiminh", [9], "French-Vietnamese baguette sandwich — global street food export."),
        ("com_lam", "Sapa bamboo rice", "sapa", [6], "Sticky rice in bamboo tube — ethnic highland specialty."),
        ("nem_lui", "Hue lemongrass skewers", "hue", [], "Royal city finger food with peanut sauce."),
        ("goi_cuon", "Fresh spring rolls national", "vietnam", list(TOURS.values()), "Universal light appetizer — elderly-friendly food format."),
        ("ca_phe", "Vietnamese coffee culture", "dalat", [2], "Robusta drip coffee and egg coffee — Da Lat cafe tourism."),
    ]
    for _, title, city, tids, content in foods:
        add(title=title, content=content,
            city_keys=[city], region="Culinary",
            interests=["food", "culture"], personas=["foreigner", "couple", "family"],
            tour_ids=tids, chunk_type="food", heritage="Intangible Cultural Practice")

    # === Accessibility per destination (extra) ===
    access = [
        ("hoi_an", "Hoi An flat terrain elderly access", [3], "Ancient town core is pedestrian flat — wheelchair partial access."),
        ("ha_long", "Ha Long cruise mobility", [4], "Select operators with cabin elevators and minimal kayak-only routes."),
        ("phu_quoc", "Phu Quoc resort accessibility", [1, 10], "International resorts offer pool lifts and ground-floor rooms."),
        ("da_nang", "Da Nang family theme access", [8], "Ba Na Hills cable car eliminates uphill climb for strollers."),
        ("hanoi", "Hanoi rest-stop walking tours", [11], "Old Quarter tours with cafe breaks every 30 minutes recommended."),
    ]
    for city, title, tids, content in access:
        add(title=title, content=content, city_keys=[city],
            region="Accessibility", interests=["relax"], personas=["elderly", "children", "family"],
            tour_ids=tids, chunk_type="accessibility", heritage="Curated")

    corpus = {
        "version": "2.0-rag-conference",
        "description": (
            "StayHub Vietnam RAG corpus for multi-source knowledge augmentation (FCAHR). "
            "Structured retrieval chunks with UNESCO citations, interest/persona tags, and tour linkage. "
            "Designed for ML.NET semantic retrieval + cultural_fit scoring."
        ),
        "paperMetadata": {
            "corpusName": "StayHub-VN-RAG-Corpus-v2",
            "targetVenue": "Q4 conference knowledge augmentation ablation",
            "sourceTypes": ["UNESCO World Heritage Centre", "VNAT", "Curated accessibility/seasonality"],
            "chunkSchema": "id, title, content, cityKeys, interestTags, personaTags, relatedTourIds, chunkType, heritageLevel, source"
        },
        "statistics": {
            "totalChunks": len(chunks),
            "unescoChunks": sum(1 for c in chunks if c.get("heritageLevel") == "UNESCO World Heritage"),
            "chunkTypes": sorted(set(c["chunkType"] for c in chunks)),
            "interestCoverage": sorted(set(t for c in chunks for t in c["interestTags"])),
            "personaCoverage": sorted(set(p for c in chunks for p in c["personaTags"]))
        },
        "chunks": chunks
    }

    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text(json.dumps(corpus, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Wrote {len(chunks)} RAG chunks to {OUT}")
    print("Stats:", json.dumps(corpus["statistics"], indent=2))


if __name__ == "__main__":
    main()
