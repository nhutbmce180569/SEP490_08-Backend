"""Shared provenance rules for StayHub tour seed data (VNAT / UNESCO)."""

VNAT_NAME = "Vietnam National Administration of Tourism"
VNAT_URL = "https://vietnamtourism.gov.vn/en"

UNESCO_NAME = "UNESCO World Heritage Centre"
UNESCO_URL = "https://whc.unesco.org/en/statesparties/vn"

# City or tour-name keyword -> (source_name, source_url, optional one-line anchor)
UNESCO_BY_KEYWORD: list[tuple[str, str, str]] = [
    ("ha long", UNESCO_NAME, "https://whc.unesco.org/en/list/672"),
    ("quang ninh", UNESCO_NAME, "https://whc.unesco.org/en/list/672"),
    ("trang an", UNESCO_NAME, "https://whc.unesco.org/en/list/1438"),
    ("ninh binh", UNESCO_NAME, "https://whc.unesco.org/en/list/1438"),
    ("tam coc", UNESCO_NAME, "https://whc.unesco.org/en/list/1438"),
    ("hoa lu", UNESCO_NAME, "https://whc.unesco.org/en/list/1438"),
    ("phong nha", UNESCO_NAME, "https://whc.unesco.org/en/list/950"),
    ("quang binh", UNESCO_NAME, "https://whc.unesco.org/en/list/950"),
    ("dong hoi", UNESCO_NAME, "https://whc.unesco.org/en/list/950"),
    ("hue", UNESCO_NAME, "https://whc.unesco.org/en/list/678"),
    ("hoi an", UNESCO_NAME, "https://whc.unesco.org/en/list/948"),
    ("my son", UNESCO_NAME, "https://whc.unesco.org/en/list/949"),
    ("ho dynasty", UNESCO_NAME, "https://whc.unesco.org/en/list/1358"),
    ("thanh hoa citadel", UNESCO_NAME, "https://whc.unesco.org/en/list/1358"),
    ("citadel of ho", UNESCO_NAME, "https://whc.unesco.org/en/list/1358"),
    ("u minh thuong", UNESCO_NAME, "https://www.unesco.org/en/mab/asia-pacific/vietnam"),
]

# Display / seed typos -> official place name used in City column
CITY_FIXES: dict[str, str] = {
    "Vung Tau Hills": "Xuyen Moc",
    "Sa Dec Flower": "Sa Dec",
    "Ha Tien Archipelago": "Ha Tien",
    "Cat Tien East": "Cat Tien",
    "Lang Co Lagoon": "Lang Co",
    "Cat Tien Lam Dong": "Cat Tien",
    "Vi Thanh Eco": "Vi Thanh",
    "Can Tho Night": "Can Tho",
    "Ba Tri Mekong": "Ba Tri",
    "Tay Ninh Border": "Tay Ninh",
    "Chau Thanh Ben Tre": "Chau Thanh",
    "Chau Thanh Hau Giang": "Chau Thanh",
    "Hoa Binh Bac Lieu": "Bac Lieu",
    "U Minh Ca Mau": "U Minh",
    "Soc Trang Khmer": "Soc Trang",
}

BOILERPLATE_FRAGMENTS = (
    "This package includes guided activities, local experiences, and flexible free time.",
    "Guided activities, local experiences, and flexible free time included.",
    "Discover ",
)

# Curated highlights for tours 113-228 (city, province, highlight) — real administrative places
CITIES_113_228: list[tuple[str, str, str]] = [
    ("Bien Hoa", "Dong Nai", "Dong Nai River parks and Long Khanh gateway"),
    ("Xuyen Moc", "Ba Ria - Vung Tau", "coastal forest trails and beach hamlets"),
    ("Long Hai", "Ba Ria - Vung Tau", "quiet beach and fishing village"),
    ("Tan An", "Long An", "Mekong gateway orchards and canals"),
    ("Tay Ninh", "Tay Ninh", "Ba Den Mountain and Cao Dai heritage"),
    ("Thu Dau Mot", "Binh Duong", "temples and southern craft villages"),
    ("Di An", "Binh Duong", "urban green belts and local cuisine"),
    ("Binh Duong", "Binh Duong", "craft villages and Thu Dau Mot culture"),
    ("Vung Liem", "Vinh Long", "canals and fruit gardens"),
    ("Sa Dec", "Dong Thap", "flower village and river life"),
    ("Tan Chau", "An Giang", "silk village and Chau Doc gateway"),
    ("Tinh Bien", "An Giang", "border market and palm scenery"),
    ("Ha Tien", "Kien Giang", "coast, islands and seafood"),
    ("Nam Du", "Kien Giang", "island hopping and coral reefs"),
    ("U Minh", "Ca Mau", "mangrove forest and biodiversity"),
    ("Vi Thanh", "Hau Giang", "wetlands and local markets"),
    ("Vi Thuy", "Hau Giang", "eco village and river cruise"),
    ("Cai Be", "Tien Giang", "floating market and homestay"),
    ("Go Cong Dong", "Tien Giang", "coast and rural culture"),
    ("Tan Phu", "Dong Nai", "Dambri waterfalls and forest trek"),
    ("Cat Tien", "Dong Nai", "Cat Tien National Park wildlife"),
    ("Bao Vinh", "Hue", "ancient port and craft alleys"),
    ("Lang Co", "Thua Thien Hue", "lagoon kayak and seafood"),
    ("A Luoi", "Thua Thien Hue", "Pa Co ethnic culture and waterfalls"),
    ("Khe Sanh", "Quang Tri", "DMZ history and highland scenery"),
    ("Lao Bao", "Quang Tri", "border town and nature"),
    ("Dien Ban", "Quang Nam", "countryside and pottery"),
    ("Thanh My", "Quang Nam", "rural trails and rice fields"),
    ("Mo Duc", "Quang Nam", "coastal dunes and fishing"),
    ("Sa Huynh", "Quang Ngai", "Champa culture and beaches"),
    ("Binh Son", "Quang Ngai", "volcanic coast and Ly Son ferry"),
    ("Duc Pho", "Quang Ngai", "green coast and local food"),
    ("An Khe", "Gia Lai", "An Khe Lake and highland breeze"),
    ("Ayun Pa", "Gia Lai", "ethnic markets and waterfalls"),
    ("Krong Pa", "Gia Lai", "tea hills and scenic passes"),
    ("Lak Lake", "Dak Lak", "elephant lake and ethnic villages"),
    ("Ea Sup", "Dak Lak", "wilderness and bird watching"),
    ("Cu Jut", "Dak Nong", "waterfalls and geopark trails"),
    ("Di Linh", "Lam Dong", "tea hills and waterfalls"),
    ("Don Duong", "Lam Dong", "flower farms and lakes"),
    ("Bao Loc", "Lam Dong", "tea plateau and waterfalls"),
    ("Cam Lam", "Khanh Hoa", "rural coast and salt fields"),
    ("Van Ninh", "Khanh Hoa", "quiet beaches and lagoons"),
    ("Dien Khanh", "Khanh Hoa", "rural heritage near Nha Trang"),
    ("Ninh Hoa", "Khanh Hoa", "countryside and pottery"),
    ("Bac Ai", "Ninh Thuan", "mountain Cham culture"),
    ("Ninh Phuoc", "Ninh Thuan", "vineyards and coastal dunes"),
    ("Thuan Bac", "Ninh Thuan", "sheep fields and photography"),
    ("La Gi", "Binh Thuan", "beach town and harbor food"),
    ("Ham Thuan Nam", "Binh Thuan", "dragon fruit farms and coast"),
    ("Duc Linh", "Binh Thuan", "rural markets and scenery"),
    ("Tanh Linh", "Binh Thuan", "forest and minority culture"),
    ("Loc Thanh", "Binh Phuoc", "rubber forest and history"),
    ("Bu Dang", "Binh Phuoc", "waterfalls and border nature"),
    ("Hon Quan", "Binh Phuoc", "lake and eco trails"),
    ("Chon Thanh", "Binh Duong", "rural temples and food"),
    ("Bau Bang", "Binh Duong", "industrial heritage tour"),
    ("Trang Bang", "Tay Ninh", "historical sites and cuisine"),
    ("Tan Bien", "Tay Ninh", "border nature and trails"),
    ("Chau Thanh", "Long An", "river orchards and cycling"),
    ("Ben Luc", "Long An", "canals and village life"),
    ("Can Giuoc", "Long An", "wetlands and birding"),
    ("Go Quao", "Kien Giang", "Mekong countryside and food"),
    ("Giong Rieng", "Kien Giang", "floating village culture"),
    ("Hong Ngu", "Dong Thap", "border river and markets"),
    ("Tam Nong", "Dong Thap", "Tram Chim bird sanctuary and lotus"),
    ("Thap Muoi", "Dong Thap", "wetland eco tour"),
    ("Lai Vung", "Dong Thap", "mandarin gardens and river"),
    ("Mo Cay", "Ben Tre", "coconut crafts and canals"),
    ("Chau Thanh", "Ben Tre", "homestay and orchard"),
    ("Ba Tri", "Ben Tre", "coastal countryside"),
    ("Binh Dai", "Ben Tre", "seafood and rural roads"),
    ("Cang Long", "Tra Vinh", "Khmer culture and pagodas"),
    ("Tieu Can", "Tra Vinh", "wetlands and village"),
    ("Tra Cu", "Tra Vinh", "coastal Khmer heritage"),
    ("Vinh Loi", "Bac Lieu", "bird sanctuary and wind farm"),
    ("Bac Lieu", "Bac Lieu", "music heritage and food"),
    ("U Minh Thuong", "Kien Giang", "UNESCO biosphere wetland"),
    ("An Bien", "Kien Giang", "mangrove and fishing"),
    ("Kien Hai", "Kien Giang", "island district exploration"),
    ("Gia Rai", "Bac Lieu", "countryside and shrimp farms"),
    ("Phuoc Long", "Bac Lieu", "rural temples and markets"),
    ("Hong Dan", "Bac Lieu", "coastal villages"),
    ("Dong Hai", "Bac Lieu", "sea breeze and seafood"),
    ("Hoa Thanh", "Ca Mau", "wetland villages"),
    ("Tran Van Thoi", "Ca Mau", "southern cape scenery"),
    ("Thoi Binh", "Ca Mau", "mangrove channels"),
    ("Nam Can", "Ca Mau", "southern landmark"),
    ("Ngoc Hien", "Ca Mau", "mangrove eco and birds"),
    ("Dam Doi", "Ca Mau", "coastal shrimp culture"),
    ("Cai Nuoc", "Ca Mau", "river life and markets"),
    ("Phu Tan", "Ca Mau", "orchards and canals"),
    ("U Minh", "Ca Mau", "U Minh Ha National Park core zone"),
    ("Vi Thanh", "Hau Giang", "urban Mekong culture"),
    ("Long My", "Hau Giang", "wetland trails"),
    ("Phung Hiep", "Hau Giang", "floating markets"),
    ("Chau Thanh", "Hau Giang", "fruit orchards"),
    ("Ba Tri", "Ben Tre", "delta immersion weekend"),
    ("Tan Phu Dong", "Tien Giang", "island orchards"),
    ("Cai Lay", "Tien Giang", "craft villages and river"),
    ("Cho Lach", "Ben Tre", "fruit kingdom tour"),
    ("Mo Cay Nam", "Ben Tre", "eco canals"),
    ("Binh Tan", "Vinh Long", "rural discovery"),
    ("Tra On", "Vinh Long", "river gardens"),
    ("Tam Binh", "Vinh Long", "quiet delta escape"),
    ("Vinh Thanh", "Can Tho", "suburban Mekong"),
    ("Co Do", "Can Tho", "orchards and canals"),
    ("Thoi Lai", "Can Tho", "countryside cycling"),
    ("O Mon", "Can Tho", "floating market extension"),
    ("Phong Dien", "Can Tho", "eco garden tour"),
    ("Thot Not", "Can Tho", "Bang Lang stork sanctuary"),
    ("Vi Thanh", "Hau Giang", "community-based tourism"),
    ("Nga Bay", "Hau Giang", "floating market half-day"),
    ("Can Tho", "Can Tho", "night market and riverfront"),
    ("Soc Trang", "Soc Trang", "Khmer pagoda trail"),
    ("My Tu", "Soc Trang", "delta food tour"),
    ("Ke Sach", "Soc Trang", "countryside homestay"),
    ("Cu Lao Dung", "Soc Trang", "island seafood"),
    ("Vinh Chau", "Soc Trang", "coast and culture"),
    ("Tran De", "Soc Trang", "estuary scenery"),
    ("Long Phu", "Soc Trang", "temple and village"),
]

# Tours 1-12: explicit VNAT/UNESCO-backed descriptions
TOURS_1_12: dict[int, tuple[str, str, str, str]] = {
    1: (
        "Phu Quoc",
        "Phu Quoc island tour: Sao Beach, An Thoi fishing port, and night market — aligned with Kien Giang coastal tourism.",
        VNAT_NAME,
        VNAT_URL,
    ),
    2: (
        "Da Lat",
        "Da Lat highlands: Xuan Huong Lake, Lam Vien Square, and night market — Lam Dong plateau tourism.",
        VNAT_NAME,
        VNAT_URL,
    ),
    3: (
        "Hoi An",
        "Hoi An Ancient Town walking tour, Cao Lau tasting, and lantern release — UNESCO historic town.",
        UNESCO_NAME,
        "https://whc.unesco.org/en/list/948",
    ),
    4: (
        "Quang Ninh",
        "Ha Long Bay overnight cruise, Sung Sot Cave, and kayaking — UNESCO natural heritage.",
        UNESCO_NAME,
        "https://whc.unesco.org/en/list/672",
    ),
    5: (
        "Can Tho",
        "Mekong Delta: Cai Rang floating market, fruit orchards, and banh xeo — VNAT Mekong hub.",
        VNAT_NAME,
        VNAT_URL,
    ),
    6: (
        "Lao Cai",
        "Sapa trekking: Cat Cat Village, terraced fields, and Fansipan cable car area.",
        VNAT_NAME,
        VNAT_URL,
    ),
    7: (
        "Ninh Binh",
        "Trang An boat ride, Mua Cave viewpoint, and Hoa Lu ancient capital — UNESCO landscape.",
        UNESCO_NAME,
        "https://whc.unesco.org/en/list/1438",
    ),
    8: (
        "Da Nang",
        "Da Nang city tour, Dragon Bridge, and Ba Na Hills Golden Bridge — central coast tourism.",
        VNAT_NAME,
        VNAT_URL,
    ),
    9: (
        "Ho Chi Minh City",
        "Saigon evening street-food tour by motorbike in District 1 — VNAT city tourism.",
        VNAT_NAME,
        VNAT_URL,
    ),
    10: (
        "Cam Ranh",
        "Cam Ranh honeymoon: Binh Ba Island, private beach, and spa — Khanh Hoa coast.",
        VNAT_NAME,
        VNAT_URL,
    ),
    11: (
        "Hanoi",
        "Hanoi Old Quarter photo walk, Hoan Kiem Lake, bun cha and egg coffee.",
        VNAT_NAME,
        VNAT_URL,
    ),
    12: (
        "Nha Trang",
        "Nha Trang island hopping, diving, and banh can — Khanh Hoa sea tourism.",
        VNAT_NAME,
        VNAT_URL,
    ),
}


def resolve_source(city: str, name: str, description: str) -> tuple[str, str]:
    blob = f"{city} {name} {description}".lower()
    for keyword, source_name, url in UNESCO_BY_KEYWORD:
        if keyword in blob:
            return source_name, url
    return VNAT_NAME, VNAT_URL


def provenance_suffix(source_name: str, source_url: str) -> str:
    return f" Source: {source_name} ({source_url})."


def strip_boilerplate(desc: str) -> str:
    d = desc.strip()
    for frag in BOILERPLATE_FRAGMENTS:
        if frag == "Discover ":
            continue
        d = d.replace(frag, "").strip()
    if d.startswith("Discover "):
        # keep only the clause after "with"
        rest = d[9:]
        if " with " in rest:
            city_part, _, tail = rest.partition(" with ")
            d = f"{city_part.strip()}: {tail.rstrip('.')}."
        else:
            d = rest.rstrip(".") + "."
    return d.strip().rstrip(".") + "." if d else d


def enrich_description(desc: str, source_name: str, source_url: str) -> str:
    d = strip_boilerplate(desc)
    if "Source:" in d:
        return d
    if not d.endswith("."):
        d += "."
    return d + provenance_suffix(source_name, source_url)
