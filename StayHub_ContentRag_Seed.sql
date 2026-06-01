-- RAG / ContentAPI tourism knowledge expansion (chạy trên StayHub_ContentDb)
USE StayHub_ContentDb;
GO

IF NOT EXISTS (SELECT 1 FROM TourismInformation WHERE Id = 19)
BEGIN
    SET IDENTITY_INSERT TourismInformation ON;
    INSERT INTO TourismInformation (Id, Name, Type, Description, Address, City, Country, Latitude, Longitude, ImageUrl, SourceName, SourceUrl, Status) VALUES
    (19, N'My Son Sanctuary', 'Heritage', N'UNESCO Cham tower complex from 4th-13th century near Hoi An.', N'Duy Phu', N'Hoi An', N'Vietnam', 15.7644, 108.1242, 'https://cdn.stayhub.vn/tourism/my-son.jpg', N'UNESCO World Heritage Centre', 'https://whc.unesco.org/en/list/949', 'Active'),
    (20, N'Perfume River Hue', 'Heritage', N'Perfume River boat trips linking imperial monuments in Hue.', N'Huong River', N'Hue', N'Vietnam', 16.4637, 107.5909, 'https://cdn.stayhub.vn/tourism/perfume-river.jpg', N'UNESCO - Complex of Hue Monuments', 'https://whc.unesco.org/en/list/678', 'Active'),
    (21, N'Imperial Citadel Hue', 'Heritage', N'Nguyen dynasty imperial citadel within UNESCO monument complex.', N'Hue City', N'Hue', N'Vietnam', 16.4697, 107.5794, 'https://cdn.stayhub.vn/tourism/hue-citadel.jpg', N'UNESCO - Complex of Hue Monuments', 'https://whc.unesco.org/en/list/678', 'Active'),
    (22, N'Japanese Covered Bridge', 'Heritage', N'Iconic 18th-century bridge in Hoi An Ancient Town UNESCO site.', N'Hoi An', N'Hoi An', N'Vietnam', 15.8770, 108.3267, 'https://cdn.stayhub.vn/tourism/japanese-bridge.jpg', N'UNESCO - Hoi An', 'https://whc.unesco.org/en/list/874', 'Active'),
    (23, N'Lantern Festival Hoi An', 'Culture', N'Monthly full-moon lantern festival in pedestrian ancient town.', N'Hoi An Ancient Town', N'Hoi An', N'Vietnam', 15.8791, 108.3270, 'https://cdn.stayhub.vn/tourism/lantern-festival.jpg', N'UNESCO - Hoi An', 'https://whc.unesco.org/en/list/874', 'Active'),
    (24, N'Water Puppet Theatre Hanoi', 'Culture', N'Traditional Vietnamese water puppetry — intangible cultural heritage performance.', N'Hoan Kiem', N'Hanoi', N'Vietnam', 21.0285, 105.8522, 'https://cdn.stayhub.vn/tourism/water-puppet.jpg', N'UNESCO Intangible Heritage', 'https://ich.unesco.org/', 'Active'),
    (25, N'Phong Nha Cave', 'Heritage', N'UNESCO Phong Nha-Ke Bang karst cave system with underground river.', N'Phong Nha', N'Quang Binh', N'Vietnam', 17.5900, 106.2833, 'https://cdn.stayhub.vn/tourism/phong-nha.jpg', N'UNESCO World Heritage Centre', 'https://whc.unesco.org/en/list/951', 'Active'),
    (26, N'Golden Bridge Ba Na', 'Destination', N'Famous pedestrian bridge held by giant hands at Ba Na Hills resort.', N'Hoa Vang', N'Da Nang', N'Vietnam', 15.9950, 107.9967, 'https://cdn.stayhub.vn/tourism/golden-bridge.jpg', N'Vietnam National Administration of Tourism', 'https://vietnamtourism.gov.vn/en', 'Active'),
    (27, N'Cau Dat Cloud Hunting', 'Activity', N'Early-morning cloud hunting viewpoint near Da Lat at Cau Dat plateau.', N'Cau Dat', N'Da Lat', N'Vietnam', 11.9290, 108.5640, 'https://cdn.stayhub.vn/tourism/cau-dat.jpg', N'Vietnam National Administration of Tourism', 'https://vietnamtourism.gov.vn/en', 'Active'),
    (28, N'Sunset Town Phu Quoc', N'Destination', N'Mediterranean-themed coastal town with sunset viewing in southern Phu Quoc.', N'An Thoi', N'Phu Quoc', N'Vietnam', 10.0800, 103.9500, 'https://cdn.stayhub.vn/tourism/sunset-town.jpg', N'Vietnam National Administration of Tourism', 'https://vietnamtourism.gov.vn/en', 'Active'),
    (29, N'Ben Thanh Market', 'LocalFood', N'Historic Saigon market for street food and local specialties.', N'District 1', N'Ho Chi Minh City', N'Vietnam', 10.7725, 106.6980, 'https://cdn.stayhub.vn/tourism/ben-thanh.jpg', N'Vietnam National Administration of Tourism', 'https://vietnamtourism.gov.vn/en', 'Active'),
    (30, N'Hoan Kiem Lake', 'Destination', N'Central Hanoi lake with Ngoc Son Temple — city walking hub.', N'Hoan Kiem', N'Hanoi', N'Vietnam', 21.0287, 105.8525, 'https://cdn.stayhub.vn/tourism/hoan-kiem.jpg', N'UNESCO - Thang Long Citadel context', 'https://whc.unesco.org/en/list/1328', 'Active'),
    (31, N'Ponagar Cham Towers', 'Heritage', N'Cham Hindu temple towers overlooking Nha Trang bay.', N'Nha Trang', N'Nha Trang', N'Vietnam', 12.2654, 109.1956, 'https://cdn.stayhub.vn/tourism/ponagar.jpg', N'Vietnam National Administration of Tourism', 'https://vietnamtourism.gov.vn/en', 'Active'),
    (32, N'Hon Mun Marine Reserve', 'Destination', N'Marine protected area popular for snorkeling near Nha Trang.', N'Nha Trang Bay', N'Nha Trang', N'Vietnam', 12.1667, 109.2833, 'https://cdn.stayhub.vn/tourism/hon-mun.jpg', N'Vietnam National Administration of Tourism', 'https://vietnamtourism.gov.vn/en', 'Active'),
    (33, N'Cat Ba Archipelago', 'Heritage', N'Cat Ba is part of Ha Long Bay UNESCO property with national park trekking.', N'Cat Ba Island', N'Quang Ninh', N'Vietnam', 20.7275, 107.0448, 'https://cdn.stayhub.vn/tourism/cat-ba.jpg', N'UNESCO - Ha Long Bay', 'https://whc.unesco.org/en/list/672', 'Active'),
    (34, N'Mui Ne Sand Dunes', 'Destination', N'Red and white sand dunes for photography and jeep tours in Binh Thuan.', N'Mui Ne', N'Phan Thiet', N'Vietnam', 10.9380, 108.2870, 'https://cdn.stayhub.vn/tourism/mui-ne-dunes.jpg', N'Vietnam National Administration of Tourism', 'https://vietnamtourism.gov.vn/en', 'Active'),
    (35, N'Cu Chi Tunnels', 'Heritage', N'Historic tunnel network day trip from Ho Chi Minh City.', N'Cu Chi', N'Ho Chi Minh City', N'Vietnam', 11.1520, 106.4940, 'https://cdn.stayhub.vn/tourism/cu-chi.jpg', N'Vietnam National Administration of Tourism', 'https://vietnamtourism.gov.vn/en', 'Active');
    SET IDENTITY_INSERT TourismInformation OFF;
    PRINT 'Inserted TourismInformation RAG seed (19-35)';
END
ELSE
    PRINT 'TourismInformation RAG seed already applied';
GO
