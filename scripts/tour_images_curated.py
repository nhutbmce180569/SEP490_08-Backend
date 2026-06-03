"""Verified Wikimedia Commons thumbnails for StayHub tour seed images."""

# Regional fallbacks — Wikimedia Commons (verified thumb URLs)
NORTH_MOUNTAIN = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/4/46/"
    "Ma_Pi_Leng_Pass_winding_road_Ha_Giang_Vietnam.jpg/"
    "960px-Ma_Pi_Leng_Pass_winding_road_Ha_Giang_Vietnam.jpg"
)
NORTH_BAY = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2d/"
    "Halong_Bay_in_Vietnam.jpg/960px-Halong_Bay_in_Vietnam.jpg"
)
RED_RIVER = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/5/53/"
    "Hoan_Kiem_Lake,_Hanoi,_Vietnam.jpg/960px-Hoan_Kiem_Lake,_Hanoi,_Vietnam.jpg"
)
CENTRAL_HERITAGE = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/5/55/"
    "Rice_Farmer_in_Hoi_An%2C_Vietnam.jpg/960px-Rice_Farmer_in_Hoi_An%2C_Vietnam.jpg"
)
CENTRAL_COAST = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/1/12/"
    "Da-Nang_Vietnam_Coracles-01.jpg/960px-Da-Nang_Vietnam_Coracles-01.jpg"
)
CAVES = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/b/bc/"
    "Phong_Nha-Ke_Bang_cave3.jpg/960px-Phong_Nha-Ke_Bang_cave3.jpg"
)
HIGHLANDS = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/6/67/"
    "Da_Lat_train_station_02.JPG/960px-Da_Lat_train_station_02.JPG"
)
SOUTH_CITY = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/f/f5/"
    "Ho_Chi_Minh_City_Collage.JPG/960px-Ho_Chi_Minh_City_Collage.JPG"
)
MEKONG = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c6/"
    "Can_Tho%2C_Vietnam%2C_Floating_Market%2C_Sale.jpg/"
    "960px-Can_Tho%2C_Vietnam%2C_Floating_Market%2C_Sale.jpg"
)
ISLAND = (
    "https://upload.wikimedia.org/wikipedia/commons/thumb/3/33/"
    "Kem_Beach_aerial_view_Phu_Quoc_Island_Vietnam.jpg/"
    "960px-Kem_Beach_aerial_view_Phu_Quoc_Island_Vietnam.jpg"
)
BEACH_RESORT = (
    "https://upload.wikimedia.org/wikipedia/commons/4/4d/"
    "Soccer_by_the_beach%2C_Nha_Trang%2C_Vietnam.jpg"
)

CURATED: dict[str, str] = {
    "Phu Quoc": ISLAND,
    "Ha Tien": ISLAND,
    "Nam Du": ISLAND,
    "Kien Hai": ISLAND,
    "Con Dao": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/3/3d/"
        "C%C3%B4n_%C4%90%E1%BA%A3o_National_Park.jpg/"
        "960px-C%C3%B4n_%C4%90%E1%BA%A3o_National_Park.jpg"
    ),
    "Quang Ninh": NORTH_BAY,
    "Cat Ba": NORTH_BAY,
    "Co To": NORTH_BAY,
    "Van Don": NORTH_BAY,
    "Lan Ha Bay": NORTH_BAY,
    "Bai Tu Long": NORTH_BAY,
    "Yen Tu": NORTH_BAY,
    "Mong Cai": NORTH_BAY,
    "Hanoi": RED_RIVER,
    "Duong Lam": RED_RIVER,
    "Ba Vi": RED_RIVER,
    "Tam Dao": RED_RIVER,
    "Hai Phong": NORTH_BAY,
    "Ninh Binh": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/"
        "Goats_in_Ninh_H%E1%BA%A3i%2C_Ninh_Binh_province%2C_Vietnam%2C_20240201_1521_4871.jpg/"
        "960px-Goats_in_Ninh_H%E1%BA%A3i%2C_Ninh_Binh_province%2C_Vietnam%2C_20240201_1521_4871.jpg"
    ),
    "Tam Coc": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/"
        "Goats_in_Ninh_H%E1%BA%A3i%2C_Ninh_Binh_province%2C_Vietnam%2C_20240201_1521_4871.jpg/"
        "960px-Goats_in_Ninh_H%E1%BA%A3i%2C_Ninh_Binh_province%2C_Vietnam%2C_20240201_1521_4871.jpg"
    ),
    "Cuc Phuong": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/"
        "Goats_in_Ninh_H%E1%BA%A3i%2C_Ninh_Binh_province%2C_Vietnam%2C_20240201_1521_4871.jpg/"
        "960px-Goats_in_Ninh_H%E1%BA%A3i%2C_Ninh_Binh_province%2C_Vietnam%2C_20240201_1521_4871.jpg"
    ),
    "Ha Giang": NORTH_MOUNTAIN,
    "Dong Van": NORTH_MOUNTAIN,
    "Cao Bang": NORTH_MOUNTAIN,
    "Bac Ha": NORTH_MOUNTAIN,
    "Y Ty": NORTH_MOUNTAIN,
    "Mu Cang Chai": NORTH_MOUNTAIN,
    "Lai Chau": NORTH_MOUNTAIN,
    "Dien Bien Phu": NORTH_MOUNTAIN,
    "Sapa": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/"
        "Rice_terraces_in_Sapa%2C_Vietnam.jpg/960px-Rice_terraces_in_Sapa%2C_Vietnam.jpg"
    ),
    "Lao Cai": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/"
        "Rice_terraces_in_Sapa%2C_Vietnam.jpg/960px-Rice_terraces_in_Sapa%2C_Vietnam.jpg"
    ),
    "Mai Chau": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e9/"
        "Laundry-Lady-in-Mai-Chau%2C-Vietnam.jpg/960px-Laundry-Lady-in-Mai-Chau%2C-Vietnam.jpg"
    ),
    "Pu Luong": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e9/"
        "Laundry-Lady-in-Mai-Chau%2C-Vietnam.jpg/960px-Laundry-Lady-in-Mai-Chau%2C-Vietnam.jpg"
    ),
    "Ba Be": NORTH_MOUNTAIN,
    "Moc Chau": HIGHLANDS,
    "Hue": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/12/"
        "Hue_Vietnam_Citadel-of-Hu%E1%BA%BF-21.jpg/960px-Hue_Vietnam_Citadel-of-Hu%E1%BA%BF-21.jpg"
    ),
    "Bao Vinh": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/12/"
        "Hue_Vietnam_Citadel-of-Hu%E1%BA%BF-21.jpg/960px-Hue_Vietnam_Citadel-of-Hu%E1%BA%BF-21.jpg"
    ),
    "Lang Co": CENTRAL_COAST,
    "A Luoi": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/12/"
        "Hue_Vietnam_Citadel-of-Hu%E1%BA%BF-21.jpg/960px-Hue_Vietnam_Citadel-of-Hu%E1%BA%BF-21.jpg"
    ),
    "Khe Sanh": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4e/"
        "Phong_Nha_Ke_Bang_National_Park.jpg/960px-Phong_Nha_Ke_Bang_National_Park.jpg"
    ),
    "Quang Tri": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4e/"
        "Phong_Nha_Ke_Bang_National_Park.jpg/960px-Phong_Nha_Ke_Bang_National_Park.jpg"
    ),
    "Lao Bao": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4e/"
        "Phong_Nha_Ke_Bang_National_Park.jpg/960px-Phong_Nha_Ke_Bang_National_Park.jpg"
    ),
    "Hoi An": CENTRAL_HERITAGE,
    "Cham Islands": CENTRAL_HERITAGE,
    "My Son": CENTRAL_HERITAGE,
    "Dien Ban": CENTRAL_HERITAGE,
    "Da Nang": CENTRAL_COAST,
    "Son Tra": CENTRAL_COAST,
    "Quang Binh": CAVES,
    "Phong Nha": CAVES,
    "Dong Hoi": CAVES,
    "Da Lat": HIGHLANDS,
    "Bao Loc": HIGHLANDS,
    "Da Teh": HIGHLANDS,
    "Mang Den": HIGHLANDS,
    "Di Linh": HIGHLANDS,
    "Don Duong": HIGHLANDS,
    "Loc Thanh": HIGHLANDS,
    "Nha Trang": BEACH_RESORT,
    "Cam Ranh": BEACH_RESORT,
    "Cam Lam": BEACH_RESORT,
    "Dien Khanh": BEACH_RESORT,
    "Van Ninh": BEACH_RESORT,
    "Ninh Hoa": BEACH_RESORT,
    "Binh Hung": BEACH_RESORT,
    "Phan Thiet": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2e/"
        "Vietnam%2C_Mui_Ne_sand_dune.jpg/960px-Vietnam%2C_Mui_Ne_sand_dune.jpg"
    ),
    "Mui Ne": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2e/"
        "Vietnam%2C_Mui_Ne_sand_dune.jpg/960px-Vietnam%2C_Mui_Ne_sand_dune.jpg"
    ),
    "Vung Tau": BEACH_RESORT,
    "Ho Tram": BEACH_RESORT,
    "Long Hai": BEACH_RESORT,
    "Ba Ria Countryside": BEACH_RESORT,
    "Xuyen Moc": BEACH_RESORT,
    "Ho Chi Minh City": SOUTH_CITY,
    "Cu Chi": SOUTH_CITY,
    "Can Gio": MEKONG,
    "Tay Ninh": SOUTH_CITY,
    "Can Tho": MEKONG,
    "My Tho": MEKONG,
    "Ben Tre": MEKONG,
    "Vinh Long": MEKONG,
    "Tra Vinh": MEKONG,
    "Soc Trang": MEKONG,
    "Bac Lieu": MEKONG,
    "Ca Mau": MEKONG,
    "An Giang": MEKONG,
    "Chau Doc": MEKONG,
    "Dong Thap": MEKONG,
    "Rach Gia": MEKONG,
    "U Minh": MEKONG,
    "U Minh Thuong": MEKONG,
    "Phu Yen": CENTRAL_COAST,
    "Quy Nhon": CENTRAL_COAST,
    "Tuy Hoa": CENTRAL_COAST,
    "Ly Son": CENTRAL_COAST,
    "Ly Son - Sa Huynh": CENTRAL_COAST,
    "Sa Huynh": CENTRAL_COAST,
    "Duc Pho": CENTRAL_COAST,
    "Binh Son": CENTRAL_COAST,
    "Mo Duc": CENTRAL_COAST,
    "Pleiku": HIGHLANDS,
    "Kon Tum": HIGHLANDS,
    "Buon Ma Thuot": HIGHLANDS,
    "Dak Nong": HIGHLANDS,
    "Lak Lake": HIGHLANDS,
    "Dak Lak Waterfalls": HIGHLANDS,
    "Cat Tien": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/9/90/"
        "Vietnam%2C_Phong_Dien%2C_Mekong_Delta%2C_River.jpg/"
        "960px-Vietnam%2C_Phong_Dien%2C_Mekong_Delta%2C_River.jpg"
    ),
    "Tri An Lake": SOUTH_CITY,
    "Bien Hoa": SOUTH_CITY,
    "Di An": SOUTH_CITY,
    "Thu Dau Mot": SOUTH_CITY,
    "Binh Duong": SOUTH_CITY,
    "Bau Bang": SOUTH_CITY,
    "Hon Quan": SOUTH_CITY,
    "Trang Bang": SOUTH_CITY,
    "Tan Bien": SOUTH_CITY,
    "Tan Chau": MEKONG,
    "Tinh Bien": MEKONG,
    "Sam Son": CAVES,
    "Cua Lo": CAVES,
    "Nghe An": CAVES,
    "Ha Tinh": CAVES,
    "Bac Ninh": RED_RIVER,
    "Nam Dinh": RED_RIVER,
    "Thai Binh": RED_RIVER,
    "Bach Ma": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/12/"
        "Hue_Vietnam_Citadel-of-Hu%E1%BA%BF-21.jpg/960px-Hue_Vietnam_Citadel-of-Hu%E1%BA%BF-21.jpg"
    ),
    "Tam Ky": CENTRAL_HERITAGE,
    "Ninh Thuan": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2e/"
        "Vietnam%2C_Mui_Ne_sand_dune.jpg/960px-Vietnam%2C_Mui_Ne_sand_dune.jpg"
    ),
    "Thuan Bac": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2e/"
        "Vietnam%2C_Mui_Ne_sand_dune.jpg/960px-Vietnam%2C_Mui_Ne_sand_dune.jpg"
    ),
    "Ninh Phuoc": (
        "https://upload.wikimedia.org/wikipedia/commons/thumb/2/2e/"
        "Vietnam%2C_Mui_Ne_sand_dune.jpg/960px-Vietnam%2C_Mui_Ne_sand_dune.jpg"
    ),
}

# Province (from Address tail) -> hub city key in CURATED
PROVINCE_HUB: dict[str, str] = {
    "Kien Giang": "Phu Quoc",
    "Ba Ria - Vung Tau": "Vung Tau",
    "Binh Thuan": "Phan Thiet",
    "Khanh Hoa": "Nha Trang",
    "Lam Dong": "Da Lat",
    "Quang Ninh": "Quang Ninh",
    "Hai Phong": "Hai Phong",
    "Hanoi": "Hanoi",
    "Ho Chi Minh City": "Ho Chi Minh City",
    "Can Tho": "Can Tho",
    "An Giang": "An Giang",
    "Dong Thap": "Dong Thap",
    "Ca Mau": "Ca Mau",
    "Soc Trang": "Soc Trang",
    "Bac Lieu": "Bac Lieu",
    "Tra Vinh": "Tra Vinh",
    "Ben Tre": "Ben Tre",
    "Tien Giang": "My Tho",
    "Vinh Long": "Vinh Long",
    "Hau Giang": "Vi Thanh",
    "Long An": "My Tho",
    "Tay Ninh": "Tay Ninh",
    "Binh Duong": "Ho Chi Minh City",
    "Dong Nai": "Bien Hoa",
    "Da Nang": "Da Nang",
    "Quang Nam": "Hoi An",
    "Thua Thien Hue": "Hue",
    "Hue": "Hue",
    "Quang Binh": "Phong Nha",
    "Quang Tri": "Quang Tri",
    "Quang Ngai": "Ly Son",
    "Binh Dinh": "Quy Nhon",
    "Phu Yen": "Phu Yen",
    "Gia Lai": "Pleiku",
    "Kon Tum": "Kon Tum",
    "Dak Lak": "Buon Ma Thuot",
    "Dak Nong": "Dak Nong",
    "Lao Cai": "Sapa",
    "Yen Bai": "Mu Cang Chai",
    "Ha Giang": "Ha Giang",
    "Cao Bang": "Cao Bang",
    "Bac Kan": "Ba Be",
    "Hoa Binh": "Mai Chau",
    "Son La": "Moc Chau",
    "Dien Bien": "Dien Bien Phu",
    "Lai Chau": "Lai Chau",
    "Ninh Binh": "Ninh Binh",
    "Thanh Hoa": "Sam Son",
    "Nghe An": "Nghe An",
    "Ha Tinh": "Ha Tinh",
    "Nam Dinh": "Nam Dinh",
    "Thai Binh": "Thai Binh",
    "Bac Ninh": "Bac Ninh",
    "Vinh Phuc": "Tam Dao",
}

REGION_BY_PROVINCE: dict[str, str] = {
    "Ha Giang": NORTH_MOUNTAIN,
    "Cao Bang": NORTH_MOUNTAIN,
    "Lao Cai": NORTH_MOUNTAIN,
    "Yen Bai": NORTH_MOUNTAIN,
    "Lai Chau": NORTH_MOUNTAIN,
    "Dien Bien": NORTH_MOUNTAIN,
    "Son La": NORTH_MOUNTAIN,
    "Bac Kan": NORTH_MOUNTAIN,
    "Hoa Binh": NORTH_MOUNTAIN,
}


def province_from_address(addr: str) -> str | None:
    raw = addr.strip()
    if raw.startswith("N'") and raw.endswith("'"):
        raw = raw[2:-1]
    raw = raw.replace("''", "'")
    if "," not in raw:
        return None
    return raw.rsplit(",", 1)[-1].strip()


def resolve_image(city: str, addr: str) -> str:
    if city in CURATED:
        return CURATED[city]
    prov = province_from_address(addr)
    if prov:
        hub = PROVINCE_HUB.get(prov)
        if hub and hub in CURATED:
            return CURATED[hub]
        if prov in REGION_BY_PROVINCE:
            return REGION_BY_PROVINCE[prov]
    low = city.lower()
    for key, url in CURATED.items():
        if key.lower() in low or low in key.lower():
            return url
    if prov:
        p = prov.lower()
        if any(x in p for x in ("giang", "cao bang", "lai chau", "dien bien", "son la", "yen bai")):
            return NORTH_MOUNTAIN
        if any(x in p for x in ("quang ninh", "hai phong")):
            return NORTH_BAY
        if any(x in p for x in ("hanoi", "bac ninh", "hung yen", "vinh phuc")):
            return RED_RIVER
        if any(x in p for x in ("hue", "quang nam", "quang ngai", "binh dinh", "phu yen")):
            return CENTRAL_HERITAGE if "hue" in p or "quang nam" in p else CENTRAL_COAST
        if any(x in p for x in ("quang binh", "quang tri", "ha tinh", "nghe an", "thanh hoa")):
            return CAVES
        if any(x in p for x in ("lam dong", "dak", "gia lai", "kon tum")):
            return HIGHLANDS
        if any(x in p for x in ("khanh hoa", "binh thuan", "ninh thuan", "phu yen", "ba ria")):
            return BEACH_RESORT
        if any(
            x in p
            for x in (
                "can tho",
                "an giang",
                "dong thap",
                "ca mau",
                "kien giang",
                "tien giang",
                "ben tre",
                "vinh long",
                "tra vinh",
                "soc trang",
                "bac lieu",
                "hau giang",
                "long an",
            )
        ):
            return MEKONG
        if any(x in p for x in ("ho chi minh", "dong nai", "tay ninh", "binh duong", "binh phuoc")):
            return SOUTH_CITY
    return CENTRAL_COAST
