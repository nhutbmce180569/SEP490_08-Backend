/* =========================================================
   STAYHUB SAMPLE DATA INSERT SCRIPT
   Run this after executing the StayHub_DatabaseSchema.sql file
   Main Roles: Customer, Tour Manager, Staff, Admin
   ========================================================= */

/* =========================================================
   1. CONTENT DB
   ========================================================= */
USE StayHub_ContentDb;
GO

SET IDENTITY_INSERT Categories ON;
INSERT INTO Categories (Id, Name, Slug, IconUrl, Description, IsActive) VALUES
(1, N'Beach & Island', 'beach-island', 'https://cdn.stayhub.vn/icons/beach.svg', N'Beach and island tours, relaxation, and coral reef diving.', 1),
(2, N'Mountain & Nature', 'mountain-nature', 'https://cdn.stayhub.vn/icons/mountain.svg', N'Mountain tours, trekking, and nature exploration.', 1),
(3, N'Heritage & Culture', 'heritage-culture', 'https://cdn.stayhub.vn/icons/heritage.svg', N'Heritage, culture, traditional craft villages, and history tours.', 1),
(4, N'Food Tour', 'food-tour', 'https://cdn.stayhub.vn/icons/food.svg', N'Local cuisine tours and specialty experiences.', 1),
(5, N'City Break', 'city-break', 'https://cdn.stayhub.vn/icons/city.svg', N'Short-day city tours.', 1),
(6, N'Adventure', 'adventure', 'https://cdn.stayhub.vn/icons/adventure.svg', N'Adventure tours, SUP boarding, ziplining, and caving.', 1),
(7, N'Family Trip', 'family-trip', 'https://cdn.stayhub.vn/icons/family.svg', N'Tours suitable for families and children.', 1),
(8, N'Honeymoon', 'honeymoon', 'https://cdn.stayhub.vn/icons/honeymoon.svg', N'Romantic getaway tours for couples.', 1),
(9, N'Eco Tourism', 'eco-tourism', 'https://cdn.stayhub.vn/icons/eco.svg', N'Green tours, local communities, and conservation.', 1),
(10, N'Wellness Retreat', 'wellness-retreat', 'https://cdn.stayhub.vn/icons/wellness.svg', N'Health and wellness retreat tours.', 1),
(11, N'Photography Trip', 'photography-trip', 'https://cdn.stayhub.vn/icons/camera.svg', N'Photo hunting and scenic check-in tours.', 1),
(12, N'Luxury Travel', 'luxury-travel', 'https://cdn.stayhub.vn/icons/luxury.svg', N'High-end tours, resorts, and private services.', 1);
SET IDENTITY_INSERT Categories OFF;
GO

SET IDENTITY_INSERT Banners ON;
INSERT INTO Banners (Id, Title, ImageUrl, TargetUrl, Priority, IsActive) VALUES
(1, N'Vibrant Summer in Phu Quoc', 'https://cdn.stayhub.vn/banners/phu-quoc-summer.jpg', '/tours/phu-quoc', 100, 1),
(2, N'Weekend Cloud Hunting in Da Lat', 'https://cdn.stayhub.vn/banners/da-lat-cloud.jpg', '/tours/da-lat', 95, 1),
(3, N'Discover Hoi An at Night', 'https://cdn.stayhub.vn/banners/hoi-an-night.jpg', '/tours/hoi-an', 90, 1),
(4, N'Mekong Delta Tour Offers', 'https://cdn.stayhub.vn/banners/mekong.jpg', '/deals/mekong', 85, 1),
(5, N'Ha Long Luxury Cruise', 'https://cdn.stayhub.vn/banners/ha-long-cruise.jpg', '/tours/ha-long', 80, 1),
(6, N'Saigon Food Tour', 'https://cdn.stayhub.vn/banners/saigon-food.jpg', '/tours/saigon-food', 75, 1),
(7, N'Da Nang - Hoi An Combo', 'https://cdn.stayhub.vn/banners/da-nang-hoi-an.jpg', '/tours/da-nang', 70, 1),
(8, N'Trekking Sapa Rice Season', 'https://cdn.stayhub.vn/banners/sapa-rice.jpg', '/tours/sapa', 65, 1),
(9, N'Explore Ninh Binh', 'https://cdn.stayhub.vn/banners/ninh-binh.jpg', '/tours/ninh-binh', 60, 1),
(10, N'Cam Ranh Wellness Retreat', 'https://cdn.stayhub.vn/banners/cam-ranh-retreat.jpg', '/tours/cam-ranh', 55, 1);
SET IDENTITY_INSERT Banners OFF;
GO

SET IDENTITY_INSERT TourismInformation ON;
INSERT INTO TourismInformation (Id, Name, Type, Description, Address, City, Country, Latitude, Longitude, ImageUrl, SourceName, SourceUrl, Status) VALUES
(1, N'Sao Beach', 'Destination', N'Famous beach with fine white sand and clear blue water in Phu Quoc.', N'An Thoi', N'Phu Quoc', N'Vietnam', 10.0588, 104.0355, 'https://cdn.stayhub.vn/tourism/bai-sao.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/bai-sao', 'Active'),
(2, N'VinWonders Phu Quoc', 'Activity', N'Large theme park, suitable for families and groups of friends.', N'Ganh Dau', N'Phu Quoc', N'Vietnam', 10.3367, 103.8562, 'https://cdn.stayhub.vn/tourism/vinwonders.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/vinwonders', 'Active'),
(3, N'Hoi An Ancient Town', 'Heritage', N'Outstanding cultural heritage with ancient architecture and lanterns.', N'Hoi An Center', N'Hoi An', N'Vietnam', 15.8801, 108.3380, 'https://cdn.stayhub.vn/tourism/hoi-an.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/hoi-an', 'Active'),
(4, N'Hoi An Cao Lau', 'LocalFood', N'Hoi An''s signature noodle dish with chewy noodles and char siu pork.', N'Hoi An Ancient Town', N'Hoi An', N'Vietnam', 15.8798, 108.3274, 'https://cdn.stayhub.vn/tourism/cao-lau.jpg', N'StayHub Food Guide', 'https://stayhub.vn/food/cao-lau', 'Active'),
(5, N'Xuan Huong Lake', 'Destination', N'Central lake of Da Lat, perfect for walking and swan boat riding.', N'Da Lat Center', N'Da Lat', N'Vietnam', 11.9404, 108.4441, 'https://cdn.stayhub.vn/tourism/ho-xuan-huong.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/ho-xuan-huong', 'Active'),
(6, N'Da Lat Night Market', 'LocalFood', N'Famous dining and shopping area in the evening.', N'Nguyen Thi Minh Khai', N'Da Lat', N'Vietnam', 11.9409, 108.4374, 'https://cdn.stayhub.vn/tourism/cho-dem-da-lat.jpg', N'StayHub Food Guide', 'https://stayhub.vn/food/cho-dem-da-lat', 'Active'),
(7, N'Ha Long Bay', 'Heritage', N'World Natural Heritage site with thousands of limestone islands.', N'Ha Long', N'Quang Ninh', N'Vietnam', 20.9101, 107.1839, 'https://cdn.stayhub.vn/tourism/ha-long.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/ha-long', 'Active'),
(8, N'Surprise Cave (Sung Sot)', 'Destination', N'Large and famous cave in Ha Long Bay.', N'Bo Hon Island', N'Quang Ninh', N'Vietnam', 20.8449, 107.0905, 'https://cdn.stayhub.vn/tourism/sung-sot-cave.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/sung-sot', 'Active'),
(9, N'Mekong Delta Pancake (Banh Xeo)', 'LocalFood', N'Crispy golden crepe served with raw vegetables and sweet-sour fish sauce.', N'Can Tho', N'Can Tho', N'Vietnam', 10.0452, 105.7469, 'https://cdn.stayhub.vn/tourism/banh-xeo.jpg', N'StayHub Food Guide', 'https://stayhub.vn/food/banh-xeo', 'Active'),
(10, N'Cai Rang Floating Market', 'Destination', N'Typical Mekong Delta floating market with river trading activities.', N'Cai Rang', N'Can Tho', N'Vietnam', 10.0035, 105.7823, 'https://cdn.stayhub.vn/tourism/cai-rang.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/cai-rang', 'Active'),
(11, N'Fansipan', 'Destination', N'The Roof of Indochina, a famous cloud hunting spot in Sapa.', N'Sapa', N'Lao Cai', N'Vietnam', 22.3033, 103.7750, 'https://cdn.stayhub.vn/tourism/fansipan.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/fansipan', 'Active'),
(12, N'Cat Cat Village', 'Heritage', N'Ethnic cultural village with terraced rice fields landscape.', N'San Sa Ho', N'Sapa', N'Vietnam', 22.3264, 103.8391, 'https://cdn.stayhub.vn/tourism/cat-cat.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/cat-cat', 'Active'),
(13, N'Trang An', 'Heritage', N'Scenic landscape complex with rivers, caves, and limestone mountains.', N'Ninh Xuan', N'Ninh Binh', N'Vietnam', 20.2538, 105.9190, 'https://cdn.stayhub.vn/tourism/trang-an.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/trang-an', 'Active'),
(14, N'Hanoi Bun Cha', 'LocalFood', N'Famous dish with grilled pork, rice noodles, and sweet-sour fish sauce.', N'Old Quarter', N'Hanoi', N'Vietnam', 21.0333, 105.8500, 'https://cdn.stayhub.vn/tourism/bun-cha.jpg', N'StayHub Food Guide', 'https://stayhub.vn/food/bun-cha', 'Active'),
(15, N'Da Nang Dragon Bridge', 'Destination', N'Iconic bridge in Da Nang with fire-breathing shows on weekends.', N'An Hai', N'Da Nang', N'Vietnam', 16.0610, 108.2276, 'https://cdn.stayhub.vn/tourism/cau-rong.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/cau-rong', 'Active'),
(16, N'Ba Na Hills', 'Activity', N'Mountain resort with the Golden Bridge and many entertainment activities.', N'Hoa Vang', N'Da Nang', N'Vietnam', 15.9950, 107.9967, 'https://cdn.stayhub.vn/tourism/ba-na.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/ba-na', 'Active'),
(17, N'Nha Trang Mini Pancake (Banh Can)', 'LocalFood', N'Small mold-baked cakes, served with dipping sauce and seafood.', N'Nha Trang Center', N'Nha Trang', N'Vietnam', 12.2388, 109.1967, 'https://cdn.stayhub.vn/tourism/banh-can.jpg', N'StayHub Food Guide', 'https://stayhub.vn/food/banh-can', 'Active'),
(18, N'Binh Ba Island', 'Destination', N'Small island famous for its blue sea and fresh seafood.', N'Cam Ranh', N'Khanh Hoa', N'Vietnam', 11.8462, 109.2221, 'https://cdn.stayhub.vn/tourism/binh-ba.jpg', N'StayHub Local Guide', 'https://stayhub.vn/guide/binh-ba', 'Active');
SET IDENTITY_INSERT TourismInformation OFF;
GO

/* =========================================================
   2. IDENTITY DB
   ========================================================= */
USE StayHub_IdentityDb;
GO

SET IDENTITY_INSERT Roles ON;
INSERT INTO Roles (Id, Name, Description) VALUES
(1, 'Customer', N'Customers booking tours and using community features.'),
(2, 'Manager', N'Manages tours, departure schedules, staff, and tour content.'),
(3, 'Staff', N'Staff supporting tour operations and customer service.'),
(4, 'Admin', N'System administrator.');
SET IDENTITY_INSERT Roles OFF;
GO

-- Admin123@
SET IDENTITY_INSERT Users ON;
INSERT INTO Users (Id, Email, PasswordHash, FullName, AvatarUrl, Provider, PhoneNumber, Gender, DateOfBirth, Status, LocPrivacy, MomentPrivacy, LastOnline) VALUES
(1, 'admin@stayhub.vn', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Nguyen Minh Admin', 'https://cdn.stayhub.vn/avatars/admin.png', 'Local', '0901000001', 'Male', '1990-01-12', 'Active', 1, 1, DATEADD(MINUTE, -5, GETDATE())),
(2, 'manager.anh@stayhub.vn', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Tran Hoai Anh', 'https://cdn.stayhub.vn/avatars/manager-anh.png', 'Local', '0901000002', 'Female', '1992-04-20', 'Active', 1, 1, DATEADD(HOUR, -1, GETDATE())),
(3, 'manager.khoa@stayhub.vn', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Le Dang Khoa', 'https://cdn.stayhub.vn/avatars/manager-khoa.png', 'Local', '0901000003', 'Male', '1989-09-03', 'Active', 1, 1, DATEADD(HOUR, -2, GETDATE())),
(4, 'staff.linh@stayhub.vn', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Pham My Linh', 'https://cdn.stayhub.vn/avatars/staff-linh.png', 'Local', '0901000004', 'Female', '1998-06-18', 'Active', 1, 1, DATEADD(MINUTE, -40, GETDATE())),
(5, 'staff.nam@stayhub.vn', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Hoang Gia Nam', 'https://cdn.stayhub.vn/avatars/staff-nam.png', 'Local', '0901000005', 'Male', '1997-11-25', 'Active', 1, 1, DATEADD(HOUR, -3, GETDATE())),
(6, 'staff.thao@stayhub.vn', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Vu Minh Thao', 'https://cdn.stayhub.vn/avatars/staff-thao.png', 'Local', '0901000006', 'Female', '1996-02-09', 'Active', 1, 1, DATEADD(DAY, -1, GETDATE())),
(7, 'staff.phuc@stayhub.vn', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Dang Quoc Phuc', 'https://cdn.stayhub.vn/avatars/staff-phuc.png', 'Local', '0901000007', 'Male', '1995-12-02', 'Active', 1, 1, DATEADD(HOUR, -6, GETDATE())),
(8, 'customer.an@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Bui Thien An', 'https://cdn.stayhub.vn/avatars/customer-an.png', 'Google', '0912000008', 'Male', '2001-03-08', 'Active', 1, 1, DATEADD(MINUTE, -10, GETDATE())),
(9, 'customer.bao@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Ngo Quoc Bao', 'https://cdn.stayhub.vn/avatars/customer-bao.png', 'Local', '0912000009', 'Male', '2000-07-22', 'Active', 1, 1, DATEADD(HOUR, -5, GETDATE())),
(10, 'customer.chi@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Do Ngoc Chi', 'https://cdn.stayhub.vn/avatars/customer-chi.png', 'Local', '0912000010', 'Female', '2002-10-15', 'Active', 1, 1, DATEADD(DAY, -2, GETDATE())),
(11, 'customer.duy@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Phan Anh Duy', 'https://cdn.stayhub.vn/avatars/customer-duy.png', 'Google', '0912000011', 'Male', '1999-05-30', 'Active', 1, 0, DATEADD(HOUR, -8, GETDATE())),
(12, 'customer.giang@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Vo Huong Giang', 'https://cdn.stayhub.vn/avatars/customer-giang.png', 'Local', '0912000012', 'Female', '2001-01-19', 'Active', 0, 1, DATEADD(DAY, -3, GETDATE())),
(13, 'customer.huy@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Nguyen Duc Huy', 'https://cdn.stayhub.vn/avatars/customer-huy.png', 'Local', '0912000013', 'Male', '1998-08-11', 'Active', 1, 1, DATEADD(HOUR, -7, GETDATE())),
(14, 'customer.lan@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Le Khanh Lan', 'https://cdn.stayhub.vn/avatars/customer-lan.png', 'Google', '0912000014', 'Female', '2003-04-01', 'Active', 1, 1, DATEADD(MINUTE, -25, GETDATE())),
(15, 'customer.minh@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Tran Nhat Minh', 'https://cdn.stayhub.vn/avatars/customer-minh.png', 'Local', '0912000015', 'Male', '1997-09-17', 'Active', 1, 0, DATEADD(DAY, -4, GETDATE())),
(16, 'customer.nhi@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Hoang Yen Nhi', 'https://cdn.stayhub.vn/avatars/customer-nhi.png', 'Local', '0912000016', 'Female', '2002-12-12', 'Active', 0, 1, DATEADD(HOUR, -4, GETDATE())),
(17, 'customer.phuong@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Mai An Phuong', 'https://cdn.stayhub.vn/avatars/customer-phuong.png', 'Google', '0912000017', 'Female', '2001-06-06', 'Active', 1, 1, DATEADD(MINUTE, -55, GETDATE())),
(18, 'customer.tuan@gmail.com', 'AQAAAAIAAYagAAAAEOJCn9ZvGWB9Q+Pu2PBxc4qWCIHkofV4ePPD8LbVe1IChmZ9w/Udo6ypnUR0MgrDEA==', N'Dinh Minh Tuan', 'https://cdn.stayhub.vn/avatars/customer-tuan.png', 'Local', '0912000018', 'Male', '1996-03-27', 'Inactive', 1, 1, DATEADD(DAY, -10, GETDATE()));
SET IDENTITY_INSERT Users OFF;
GO

INSERT INTO UserRoles (UserId, RoleId) VALUES
(1, 4),
(2, 2), (3, 2),
(4, 3), (5, 3), (6, 3), (7, 3),
(8, 1), (9, 1), (10, 1), (11, 1), (12, 1), (13, 1), (14, 1), (15, 1), (16, 1), (17, 1), (18, 1),
(2, 3), (3, 3);
GO

SET IDENTITY_INSERT RefreshTokens ON;
INSERT INTO RefreshTokens (Id, UserId, Token, Expires, CreatedByIp, RevokedAt, RevokedByIp, ReplacedByToken, ReasonRevoked) VALUES
(1, 1, 'refresh_admin_001', DATEADD(DAY, 30, GETDATE()), '127.0.0.1', NULL, NULL, NULL, NULL),
(2, 2, 'refresh_manager_anh_001', DATEADD(DAY, 30, GETDATE()), '127.0.0.1', NULL, NULL, NULL, NULL),
(3, 3, 'refresh_manager_khoa_001', DATEADD(DAY, 20, GETDATE()), '127.0.0.1', NULL, NULL, NULL, NULL),
(4, 4, 'refresh_staff_linh_001', DATEADD(DAY, 15, GETDATE()), '127.0.0.1', NULL, NULL, NULL, NULL),
(5, 5, 'refresh_staff_nam_old', DATEADD(DAY, -1, GETDATE()), '127.0.0.1', DATEADD(DAY, -2, GETDATE()), '127.0.0.1', 'refresh_staff_nam_002', N'Rotated token'),
(6, 8, 'refresh_customer_an_001', DATEADD(DAY, 30, GETDATE()), '127.0.0.1', NULL, NULL, NULL, NULL),
(7, 9, 'refresh_customer_bao_001', DATEADD(DAY, 25, GETDATE()), '127.0.0.1', NULL, NULL, NULL, NULL),
(8, 10, 'refresh_customer_chi_001', DATEADD(DAY, 22, GETDATE()), '127.0.0.1', NULL, NULL, NULL, NULL),
(9, 11, 'refresh_customer_duy_001', DATEADD(DAY, 18, GETDATE()), '127.0.0.1', NULL, NULL, NULL, NULL),
(10, 12, 'refresh_customer_giang_001', DATEADD(DAY, 12, GETDATE()), '127.0.0.1', NULL, NULL, NULL, NULL);
SET IDENTITY_INSERT RefreshTokens OFF;
GO

/* =========================================================
   3. CATALOG DB
   ========================================================= */
USE StayHub_CatalogDb;
GO

SET IDENTITY_INSERT Tours ON;

INSERT INTO Tours 
(Id, CategoryId, CreatedBy, Name, Description, Country, City, Address, ImageUrl, Status) 
VALUES
(1, 1, 1, N'Phu Quoc 4D3N - Blue Sea and Sunset Town', N'Phu Quoc resort stay, visit Sao Beach, Sunset Town, and the night market.', N'Vietnam', N'Phu Quoc', N'An Thoi, Phu Quoc', 'https://cdn.stayhub.vn/tours/phu-quoc.jpg', 'Active'),

(2, 2, 1, N'Da Lat 3D2N - Cloud Hunting and Chilling', N'Experience youthful Da Lat, cloud hunting, coffee, and night market.', N'Vietnam', N'Da Lat', N'Da Lat Center', 'https://cdn.stayhub.vn/tours/da-lat.jpg', 'Active'),

(3, 3, 1, N'Hoi An 2D1N - Lanterns and Ancient Town', N'Visit the ancient town, enjoy Cao Lau, and release flower lanterns.', N'Vietnam', N'Hoi An', N'Hoi An Ancient Town', 'https://cdn.stayhub.vn/tours/hoi-an.jpg', 'Active'),

(4, 12, 1, N'Ha Long 2D1N - Luxury Cruise', N'Ha Long Bay cruise, cave visits, and dinner party.', N'Vietnam', N'Quang Ninh', N'Ha Long Marina', 'https://cdn.stayhub.vn/tours/ha-long.jpg', 'Active'),

(5, 9, 1, N'Mekong Delta 2D1N - Cai Rang Floating Market', N'Explore the river landscape, fruit orchards, and Mekong cuisine.', N'Vietnam', N'Can Tho', N'Ninh Kieu, Can Tho', 'https://cdn.stayhub.vn/tours/mekong.jpg', 'Active'),

(6, 6, 1, N'Sapa 4D3N - Village Trekking', N'Trek through terraced fields, Cat Cat Village, and Fansipan.', N'Vietnam', N'Lao Cai', N'Sapa', 'https://cdn.stayhub.vn/tours/sapa.jpg', 'Active'),

(7, 3, 1, N'Ninh Binh 2D1N - Trang An and Mua Cave', N'Explore Trang An, Mua Cave, and Hoa Lu ancient capital.', N'Vietnam', N'Ninh Binh', N'Trang An', 'https://cdn.stayhub.vn/tours/ninh-binh.jpg', 'Active'),

(8, 5, 1, N'Da Nang 3D2N - Dragon Bridge and Ba Na Hills', N'Da Nang coastal city tour, Dragon Bridge, Ba Na Hills, and Hoi An.', N'Vietnam', N'Da Nang', N'Da Nang Center', 'https://cdn.stayhub.vn/tours/da-nang.jpg', 'Active'),

(9, 4, 1, N'Saigon Evening Food Tour', N'Explore Saigon street food by motorbike.', N'Vietnam', N'Ho Chi Minh City', N'District 1', 'https://cdn.stayhub.vn/tours/saigon-food.jpg', 'Active'),

(10, 8, 1, N'Cam Ranh Honeymoon 3D2N', N'Private beach resort, spa, and romantic dinner.', N'Vietnam', N'Khanh Hoa', N'Cam Ranh', 'https://cdn.stayhub.vn/tours/cam-ranh.jpg', 'Active'),

(11, 11, 1, N'Hanoi Photo Walk 1 Day', N'Photo hunting in the Old Quarter, Hoan Kiem Lake, enjoy Bun Cha and egg coffee.', N'Vietnam', N'Hanoi', N'Hanoi Old Quarter', 'https://cdn.stayhub.vn/tours/ha-noi-photo.jpg', 'Active'),

(12, 1, 1, N'Nha Trang Island Hopping 2D1N', N'Scuba diving, Binh Ba Island, seafood, and Banh Can.', N'Vietnam', N'Nha Trang', N'Nha Trang Pier', 'https://cdn.stayhub.vn/tours/nha-trang.jpg', 'Active');

SET IDENTITY_INSERT Tours OFF;
GO


SET IDENTITY_INSERT TourItineraries ON;
INSERT INTO TourItineraries (Id, TourId, DayNumber, Title, Description, StartDuration, EndDuration, LocationName, LocationLat, LocationLng, TourismInfoId) VALUES
(1, 1, 1, N'Pick up guests and check-in to resort', N'Airport pickup, room check-in, and free time to swim.', '14:00', '17:30', N'Phu Quoc Resort', 10.2150, 103.9592, 1),
(2, 1, 2, N'Visit Sao Beach', N'Swim, eat seafood lunch, and take photos.', '08:00', '15:00', N'Sao Beach', 10.0588, 104.0355, 1),
(3, 2, 1, N'Cloud hunting at Cau Dat', N'Wake up early to hunt clouds, drink coffee, and take photos.', '04:30', '09:00', N'Cau Dat', 11.9290, 108.5640, 5),
(4, 2, 2, N'Da Lat Night Market', N'Free time to eat and buy specialties.', '18:00', '21:30', N'Da Lat Night Market', 11.9409, 108.4374, 6),
(5, 3, 1, N'Hoi An Ancient Town', N'Visit ancient houses, Japanese Covered Bridge, and lantern streets.', '15:00', '21:00', N'Hoi An Ancient Town', 15.8801, 108.3380, 3),
(6, 3, 2, N'Enjoy Cao Lau', N'Eat Hoi An specialty for lunch before the tour ends.', '11:00', '12:30', N'Hoi An', 15.8798, 108.3274, 4),
(7, 4, 1, N'Board Ha Long Cruise', N'Cruise check-in, have lunch, and enjoy the bay view.', '11:30', '17:00', N'Ha Long Bay', 20.9101, 107.1839, 7),
(8, 4, 2, N'Visit Surprise Cave', N'Explore the cave and kayak.', '08:00', '11:00', N'Surprise Cave', 20.8449, 107.0905, 8),
(9, 5, 1, N'Cai Rang Floating Market', N'Visit the floating market and have breakfast on the boat.', '05:30', '09:00', N'Cai Rang Floating Market', 10.0035, 105.7823, 10),
(10, 5, 2, N'Mekong Pancake Experience', N'Learn to make Banh Xeo and have lunch in the garden.', '10:00', '13:00', N'Can Tho', 10.0452, 105.7469, 9),
(11, 6, 1, N'Trek Cat Cat Village', N'Walk to visit the village and waterfall.', '08:00', '15:00', N'Cat Cat Village', 22.3264, 103.8391, 12),
(12, 6, 3, N'Conquer Fansipan', N'Take the Fansipan cable car and hunt clouds.', '07:00', '13:00', N'Fansipan', 22.3033, 103.7750, 11),
(13, 7, 1, N'Trang An Boat Ride', N'Take a boat through caves and admire limestone mountains.', '08:00', '11:30', N'Trang An', 20.2538, 105.9190, 13),
(14, 8, 1, N'Dragon Bridge at Night', N'See the Dragon Bridge and stroll along the Han River.', '19:00', '21:00', N'Dragon Bridge', 16.0610, 108.2276, 15),
(15, 8, 2, N'Ba Na Hills', N'Visit the Golden Bridge and the French Village.', '08:00', '16:00', N'Ba Na Hills', 15.9950, 107.9967, 16),
(16, 11, 1, N'Old Quarter Photo Walk', N'Hunt photos in the Old Quarter and enjoy Bun Cha.', '07:30', '17:00', N'Hanoi Old Quarter', 21.0333, 105.8500, 14),
(17, 12, 1, N'Nha Trang Island Hopping', N'Swim, dive to see corals, and eat seafood.', '08:00', '16:00', N'Nha Trang', 12.2388, 109.1967, 17),
(18, 12, 2, N'Binh Ba Island', N'Visit the island, check-in at the blue beach, and have lunch.', '08:00', '14:00', N'Binh Ba', 11.8462, 109.2221, 18);
SET IDENTITY_INSERT TourItineraries OFF;
GO

SET IDENTITY_INSERT TourSchedules ON;
INSERT INTO TourSchedules (Id, TourId, DepartureDate, ReturnDate, Price, MaxCapacity, SoldQuantity, AvailableSeats, Note) VALUES
(1, 1, '2026-06-01 08:00:00', '2026-06-04 18:00:00', 5490000, 30, 8, 22, N'Includes domestic flight tickets depending on the package.'),
(2, 1, '2026-06-15 08:00:00', '2026-06-18 18:00:00', 5790000, 30, 12, 18, N'Suitable schedule for families.'),
(3, 2, '2026-06-07 06:00:00', '2026-06-09 20:00:00', 2890000, 25, 15, 10, N'Bring warm clothes for cloud hunting.'),
(4, 2, '2026-07-05 06:00:00', '2026-07-07 20:00:00', 3090000, 25, 5, 20, N'Weekend schedule.'),
(5, 3, '2026-06-20 09:00:00', '2026-06-21 17:00:00', 1890000, 20, 6, 14, N'Includes flower lantern release ticket.'),
(6, 4, '2026-06-10 07:00:00', '2026-06-11 18:00:00', 4290000, 18, 10, 8, N'Cabin for 2 guests.'),
(7, 5, '2026-06-12 05:00:00', '2026-06-13 18:00:00', 1690000, 28, 18, 10, N'Departs from Can Tho.'),
(8, 6, '2026-07-01 06:00:00', '2026-07-04 20:00:00', 4990000, 22, 4, 18, N'Moderate trekking level.'),
(9, 7, '2026-06-25 07:00:00', '2026-06-26 18:00:00', 1990000, 30, 9, 21, N'Boat ride in Trang An included.'),
(10, 8, '2026-07-10 08:00:00', '2026-07-12 20:00:00', 3590000, 32, 12, 20, N'Ba Na Hills ticket included.'),
(11, 9, '2026-06-05 18:00:00', '2026-06-05 22:30:00', 790000, 12, 7, 5, N'Evening tour by motorbike.'),
(12, 10, '2026-07-20 09:00:00', '2026-07-22 12:00:00', 6990000, 16, 3, 13, N'Suitable for couples.'),
(13, 11, '2026-06-09 07:00:00', '2026-06-09 17:00:00', 990000, 15, 4, 11, N'Guide available to support photography.'),
(14, 12, '2026-06-28 08:00:00', '2026-06-29 17:00:00', 2490000, 24, 8, 16, N'Includes boat ride to the island.'),
(15, 4, '2026-07-18 07:00:00', '2026-07-19 18:00:00', 4590000, 18, 2, 16, N'Luxury cruise.'),
(16, 8, '2026-08-01 08:00:00', '2026-08-03 20:00:00', 3790000, 32, 0, 32, N'Summer schedule.');
SET IDENTITY_INSERT TourSchedules OFF;
GO

SET IDENTITY_INSERT TourScheduleItineraries ON;
INSERT INTO TourScheduleItineraries (Id, ScheduleId, DayNumber, ItineraryDate, Title, Description, StartDuration, EndDuration, LocationName, LocationLat, LocationLng, TourismInfoId) VALUES
(1, 1, 1, '2026-06-01', N'Pick up in Phu Quoc', N'Airport pickup, resort check-in.', '14:00', '17:30', N'Phu Quoc Resort', 10.2150, 103.9592, 1),
(2, 1, 2, '2026-06-02', N'Sao Beach and Sunset Town', N'Swim at Sao Beach, visit Sunset Town in the afternoon.', '08:00', '18:00', N'Sao Beach', 10.0588, 104.0355, 1),
(3, 2, 1, '2026-06-15', N'Check-in Phu Quoc', N'Check in and free time for relaxation.', '14:00', '17:30', N'Phu Quoc Resort', 10.2150, 103.9592, 1),
(4, 3, 1, '2026-06-07', N'Cloud hunting at Cau Dat', N'Hunt clouds and have breakfast.', '04:30', '09:00', N'Cau Dat', 11.9290, 108.5640, 5),
(5, 3, 2, '2026-06-08', N'Da Lat Night Market', N'Free time to explore local cuisine.', '18:00', '21:30', N'Da Lat Night Market', 11.9409, 108.4374, 6),
(6, 5, 1, '2026-06-20', N'Stroll in Hoi An Ancient Town', N'Ancient houses, Japanese Bridge, and lantern release.', '15:00', '21:00', N'Hoi An Ancient Town', 15.8801, 108.3380, 3),
(7, 5, 2, '2026-06-21', N'Hoi An Cao Lau', N'Have Cao Lau for lunch before returning.', '11:00', '12:30', N'Hoi An', 15.8798, 108.3274, 4),
(8, 6, 1, '2026-06-10', N'Boarding the Cruise', N'Receive cabin, eat lunch, and admire the bay.', '11:30', '17:00', N'Ha Long Bay', 20.9101, 107.1839, 7),
(9, 6, 2, '2026-06-11', N'Surprise Cave', N'Explore the cave and kayak.', '08:00', '11:00', N'Surprise Cave', 20.8449, 107.0905, 8),
(10, 7, 1, '2026-06-12', N'Cai Rang Floating Market', N'Have breakfast on the boat and tour the market.', '05:30', '09:00', N'Cai Rang Floating Market', 10.0035, 105.7823, 10),
(11, 7, 2, '2026-06-13', N'Make Banh Xeo', N'Experience Mekong Delta cuisine.', '10:00', '13:00', N'Can Tho', 10.0452, 105.7469, 9),
(12, 8, 1, '2026-07-01', N'Trek Cat Cat Village', N'Visit the village and terraced fields.', '08:00', '15:00', N'Cat Cat Village', 22.3264, 103.8391, 12),
(13, 8, 3, '2026-07-03', N'Fansipan', N'Conquer Fansipan by cable car.', '07:00', '13:00', N'Fansipan', 22.3033, 103.7750, 11),
(14, 9, 1, '2026-06-25', N'Trang An', N'Take a boat to visit the scenic site.', '08:00', '11:30', N'Trang An', 20.2538, 105.9190, 13),
(15, 10, 1, '2026-07-10', N'Dragon Bridge', N'Stroll the coastal city and watch the Dragon Bridge.', '19:00', '21:00', N'Dragon Bridge', 16.0610, 108.2276, 15),
(16, 10, 2, '2026-07-11', N'Ba Na Hills', N'Visit the Golden Bridge.', '08:00', '16:00', N'Ba Na Hills', 15.9950, 107.9967, 16),
(17, 13, 1, '2026-06-09', N'Hanoi Photo Walk', N'Hunt photos in the Old Quarter and enjoy Bun Cha.', '07:30', '17:00', N'Hanoi Old Quarter', 21.0333, 105.8500, 14),
(18, 14, 1, '2026-06-28', N'Nha Trang Island Hopping', N'Swim and dive to see corals.', '08:00', '16:00', N'Nha Trang', 12.2388, 109.1967, 17);
SET IDENTITY_INSERT TourScheduleItineraries OFF;
GO

SET IDENTITY_INSERT TourScheduleStaffs ON;
INSERT INTO TourScheduleStaffs (Id, ScheduleId, StaffId, AssignedRole) VALUES
(1, 1, 4, N'Lead Tour Guide'),
(2, 1, 5, N'Logistics'),
(3, 2, 6, N'Lead Tour Guide'),
(4, 3, 4, N'Lead Tour Guide'),
(5, 4, 7, N'Driver'),
(6, 5, 5, N'Local Guide'),
(7, 6, 6, N'Cruise Coordinator'),
(8, 7, 4, N'Lead Tour Guide'),
(9, 8, 7, N'Trekking Guide'),
(10, 9, 5, N'Lead Tour Guide'),
(11, 10, 6, N'Lead Tour Guide'),
(12, 11, 4, N'Food Tour Guide'),
(13, 12, 5, N'Resort Coordinator'),
(14, 13, 6, N'Photo Guide'),
(15, 14, 7, N'Island Tour Guide'),
(16, 15, 5, N'Cruise Coordinator');
SET IDENTITY_INSERT TourScheduleStaffs OFF;
GO

SET IDENTITY_INSERT Wishlists ON;
INSERT INTO Wishlists (Id, CustomerId, TourId) VALUES
(1, 8, 1), (2, 8, 2), (3, 9, 4), (4, 9, 6), (5, 10, 3),
(6, 10, 8), (7, 11, 5), (8, 12, 7), (9, 13, 10), (10, 14, 11),
(11, 15, 12), (12, 16, 1), (13, 17, 2), (14, 17, 4), (15, 13, 8);
SET IDENTITY_INSERT Wishlists OFF;
GO

SET IDENTITY_INSERT Reviews ON;
INSERT INTO Reviews (Id, CustomerId, TourId, Rating, Comment, CreatedAt, UpdatedAt) VALUES
(1, 8, 1, 5, N'Phu Quoc is very beautiful, the guide is enthusiastic, and the schedule is well-paced.', DATEADD(DAY, -20, GETUTCDATE()), NULL),
(2, 9, 4, 5, N'The Ha Long cruise is premium, food is good, and rooms are clean.', DATEADD(DAY, -18, GETUTCDATE()), NULL),
(3, 10, 3, 4, N'Hoi An is beautiful but the time was a bit short.', DATEADD(DAY, -15, GETUTCDATE()), NULL),
(4, 11, 5, 5, N'The floating market is fun, a true Mekong Delta experience.', DATEADD(DAY, -14, GETUTCDATE()), NULL),
(5, 12, 7, 4, N'Trang An is worth visiting, but Mua Cave was a bit crowded.', DATEADD(DAY, -13, GETUTCDATE()), NULL),
(6, 13, 10, 5, N'The Cam Ranh resort is beautiful, great for relaxing.', DATEADD(DAY, -12, GETUTCDATE()), NULL),
(7, 14, 11, 5, N'Hanoi photo walk is very chill, the guide knows great angles.', DATEADD(DAY, -11, GETUTCDATE()), NULL),
(8, 15, 12, 4, N'Nha Trang beach is nice, seafood is decent.', DATEADD(DAY, -10, GETUTCDATE()), NULL),
(9, 16, 2, 5, N'Da Lat cloud hunting is stunning, worth the money.', DATEADD(DAY, -9, GETUTCDATE()), NULL),
(10, 17, 8, 4, N'Ba Na is beautiful, the schedule is a bit tight but fun.', DATEADD(DAY, -8, GETUTCDATE()), NULL),
(11, 8, 6, 5, N'Sapa trekking is tiring but the scenery is extremely beautiful.', DATEADD(DAY, -7, GETUTCDATE()), NULL),
(12, 9, 9, 4, N'Food tour was delicious, should add more dishes.', DATEADD(DAY, -6, GETUTCDATE()), NULL);
SET IDENTITY_INSERT Reviews OFF;
GO

SET IDENTITY_INSERT ReviewReplies ON;
INSERT INTO ReviewReplies (Id, ReviewId, UserId, Content, CreatedAt, UpdatedAt) VALUES
(1, 1, 2, N'Thank you for trusting StayHub, see you on the next tour.', DATEADD(DAY, -19, GETUTCDATE()), NULL),
(2, 2, 3, N'We are glad you enjoyed the cruise experience.', DATEADD(DAY, -17, GETUTCDATE()), NULL),
(3, 3, 4, N'We have noted your feedback regarding the Hoi An tour duration.', DATEADD(DAY, -14, GETUTCDATE()), NULL),
(4, 4, 5, N'Thank you, the Mekong Delta in this season is truly lovely.', DATEADD(DAY, -13, GETUTCDATE()), NULL),
(5, 5, 2, N'StayHub will optimize the schedule to avoid peak crowded hours.', DATEADD(DAY, -12, GETUTCDATE()), NULL),
(6, 6, 3, N'Thank you for reviewing the Cam Ranh honeymoon tour.', DATEADD(DAY, -11, GETUTCDATE()), NULL),
(7, 7, 6, N'The guide is very happy to receive your feedback.', DATEADD(DAY, -10, GETUTCDATE()), NULL),
(8, 8, 7, N'We will add more seafood options to the itinerary.', DATEADD(DAY, -9, GETUTCDATE()), NULL),
(9, 9, 2, N'Da Lat cloud hunting is always a best seller at StayHub.', DATEADD(DAY, -8, GETUTCDATE()), NULL),
(10, 10, 4, N'Thanks for the feedback, we will adjust the pace of the schedule.', DATEADD(DAY, -7, GETUTCDATE()), NULL);
SET IDENTITY_INSERT ReviewReplies OFF;
GO

/* =========================================================
   4. BOOKING DB
   ========================================================= */
USE StayHub_BookingDb;
GO

SET IDENTITY_INSERT Orders ON;
INSERT INTO Orders (Id, CustomerId, ScheduleId, TicketCount, DiscountValue, FinalAmount, Note, Status, OrderedAt, InviteToken) VALUES
(1, 8, 1, 2, 300000, 10680000, N'Prefer a room near the beach.', 'Paid', DATEADD(DAY, -25, GETDATE()), 'INVITE_ORDER_001'),
(2, 9, 6, 2, 0, 8580000, N'1 vegetarian guest.', 'Paid', DATEADD(DAY, -22, GETDATE()), 'INVITE_ORDER_002'),
(3, 10, 5, 1, 100000, 1790000, NULL, 'Completed', DATEADD(DAY, -20, GETDATE()), 'INVITE_ORDER_003'),
(4, 11, 7, 3, 200000, 4870000, N'Traveling with a child.', 'Paid', DATEADD(DAY, -18, GETDATE()), 'INVITE_ORDER_004'),
(5, 12, 9, 2, 0, 3980000, NULL, 'Pending', DATEADD(DAY, -16, GETDATE()), 'INVITE_ORDER_005'),
(6, 13, 12, 2, 500000, 13480000, N'Honeymoon setup.', 'Paid', DATEADD(DAY, -15, GETDATE()), 'INVITE_ORDER_006'),
(7, 14, 13, 1, 0, 990000, NULL, 'Completed', DATEADD(DAY, -12, GETDATE()), 'INVITE_ORDER_007'),
(8, 15, 14, 2, 200000, 4780000, N'No spicy food.', 'Paid', DATEADD(DAY, -10, GETDATE()), 'INVITE_ORDER_008'),
(9, 16, 3, 2, 0, 5780000, NULL, 'Cancelled', DATEADD(DAY, -9, GETDATE()), 'INVITE_ORDER_009'),
(10, 17, 10, 2, 300000, 6880000, N'Need a twin room.', 'Paid', DATEADD(DAY, -8, GETDATE()), 'INVITE_ORDER_010'),
(11, 8, 8, 1, 0, 4990000, NULL, 'Pending', DATEADD(DAY, -6, GETDATE()), 'INVITE_ORDER_011'),
(12, 9, 11, 2, 0, 1580000, N'Evening food tour.', 'Paid', DATEADD(DAY, -5, GETDATE()), 'INVITE_ORDER_012'),
(13, 10, 2, 2, 500000, 11080000, N'Family of 2.', 'Paid', DATEADD(DAY, -4, GETDATE()), 'INVITE_ORDER_013'),
(14, 11, 15, 2, 0, 9180000, NULL, 'Pending', DATEADD(DAY, -3, GETDATE()), 'INVITE_ORDER_014'),
(15, 12, 4, 1, 0, 3090000, N'Front seat on the bus if possible.', 'Paid', DATEADD(DAY, -2, GETDATE()), 'INVITE_ORDER_015');
SET IDENTITY_INSERT Orders OFF;
GO

SET IDENTITY_INSERT Tickets ON;
INSERT INTO Tickets (Id, OrderId, UserId, AttendeeName, IdCard, DateOfBirth, Gender, Nationality, QrCode, CheckInStatus) VALUES
(1, 1, 8, N'Bui Thien An', '079201000001', '2001-03-08', 'Male', N'Vietnam', 'QR_TICKET_001', 'Pending'),
(2, 1, NULL, N'Nguyen Minh Quan', '079200000002', '2000-08-21', 'Male', N'Vietnam', 'QR_TICKET_002', 'Pending'),
(3, 2, 9, N'Ngo Quoc Bao', '079200000003', '2000-07-22', 'Male', N'Vietnam', 'QR_TICKET_003', 'Pending'),
(4, 2, NULL, N'Le Thao Vy', '079200000004', '2001-12-11', 'Female', N'Vietnam', 'QR_TICKET_004', 'Pending'),
(5, 3, 10, N'Do Ngoc Chi', '079200000005', '2002-10-15', 'Female', N'Vietnam', 'QR_TICKET_005', 'CheckedIn'),
(6, 4, 11, N'Phan Anh Duy', '079200000006', '1999-05-30', 'Male', N'Vietnam', 'QR_TICKET_006', 'Pending'),
(7, 4, NULL, N'Phan Minh Khang', '079200000007', '2012-02-14', 'Male', N'Vietnam', 'QR_TICKET_007', 'Pending'),
(8, 4, NULL, N'Tran My Hanh', '079200000008', '1988-09-09', 'Female', N'Vietnam', 'QR_TICKET_008', 'Pending'),
(9, 5, 12, N'Vo Huong Giang', '079200000009', '2001-01-19', 'Female', N'Vietnam', 'QR_TICKET_009', 'Pending'),
(10, 5, NULL, N'Le Gia Han', '079200000010', '2001-04-22', 'Female', N'Vietnam', 'QR_TICKET_010', 'Pending'),
(11, 6, 13, N'Nguyen Duc Huy', '079200000011', '1998-08-11', 'Male', N'Vietnam', 'QR_TICKET_011', 'Pending'),
(12, 6, NULL, N'Mai Thanh Tu', '079200000012', '1999-11-03', 'Female', N'Vietnam', 'QR_TICKET_012', 'Pending'),
(13, 7, 14, N'Le Khanh Lan', '079200000013', '2003-04-01', 'Female', N'Vietnam', 'QR_TICKET_013', 'CheckedIn'),
(14, 8, 15, N'Tran Nhat Minh', '079200000014', '1997-09-17', 'Male', N'Vietnam', 'QR_TICKET_014', 'Pending'),
(15, 8, NULL, N'Dinh Phuong Nam', '079200000015', '1996-01-01', 'Male', N'Vietnam', 'QR_TICKET_015', 'Pending'),
(16, 9, 16, N'Hoang Yen Nhi', '079200000016', '2002-12-12', 'Female', N'Vietnam', 'QR_TICKET_016', 'Pending'),
(17, 10, 17, N'Mai An Phuong', '079200000017', '2001-06-06', 'Female', N'Vietnam', 'QR_TICKET_017', 'Pending'),
(18, 10, NULL, N'Ngo Tuan Kiet', '079200000018', '2000-05-09', 'Male', N'Vietnam', 'QR_TICKET_018', 'Pending'),
(19, 12, 9, N'Ngo Quoc Bao', '079200000019', '2000-07-22', 'Male', N'Vietnam', 'QR_TICKET_019', 'Pending'),
(20, 12, NULL, N'Vu Minh Tri', '079200000020', '2001-06-16', 'Male', N'Vietnam', 'QR_TICKET_020', 'Pending');
SET IDENTITY_INSERT Tickets OFF;
GO

SET IDENTITY_INSERT CancellationRequests ON;
INSERT INTO CancellationRequests (Id, OrderId, CustomerId, BankName, AccountNumber, AccountHolderName, RequestedAt, OriginalAmount, CancellationFee, FeePercent, RefundAmount, Reason, Status, RejectReason, ProcessedAt, ProcessedBy) VALUES
(1, 9, 16, N'Vietcombank', '1023456789', N'HOANG YEN NHI', DATEADD(DAY, -8, GETDATE()), 5780000, 578000, 10, 5202000, N'Family matters, unable to attend.', 'Approved', NULL, DATEADD(DAY, -7, GETDATE()), 1),
(2, 5, 12, N'MB Bank', '2233445566', N'VO HUONG GIANG', DATEADD(DAY, -5, GETDATE()), 3980000, 398000, 10, 3582000, N'Want to change to another schedule.', 'Pending', NULL, NULL, NULL),
(3, 11, 8, N'Techcombank', '9988776655', N'BUI THIEN AN', DATEADD(DAY, -3, GETDATE()), 4990000, 499000, 10, 4491000, N'Work schedule changed.', 'Pending', NULL, NULL, NULL),
(4, 14, 11, N'ACB', '6677889900', N'PHAN ANH DUY', DATEADD(DAY, -2, GETDATE()), 9180000, 918000, 10, 8262000, N'Ordered the wrong number of tickets.', 'Rejected', N'Order is unpaid, no refund request needed.', DATEADD(DAY, -1, GETDATE()), 1),
(5, 15, 12, N'VPBank', '5566778899', N'VO HUONG GIANG', DATEADD(DAY, -1, GETDATE()), 3090000, 309000, 10, 2781000, N'Health condition is not fit for the tour.', 'Pending', NULL, NULL, NULL),
(6, 3, 10, N'VietinBank', '1112223334', N'DO NGOC CHI', DATEADD(DAY, -15, GETDATE()), 1890000, 189000, 10, 1701000, N'Tour completed but need to correct invoice information.', 'Rejected', N'Tour is completed, not eligible for cancellation.', DATEADD(DAY, -14, GETDATE()), 1),
(7, 2, 9, N'BIDV', '9990001112', N'NGO QUOC BAO', DATEADD(DAY, -4, GETDATE()), 8580000, 858000, 10, 7722000, N'Want to reschedule to next month.', 'Approved', NULL, DATEADD(DAY, -3, GETDATE()), 1),
(8, 7, 14, N'Sacombank', '1212121212', N'LE KHANH LAN', DATEADD(DAY, -9, GETDATE()), 990000, 99000, 10, 891000, N'Accidentally created a request after taking the tour.', 'Rejected', N'Ticket already checked-in and tour completed.', DATEADD(DAY, -8, GETDATE()), 1),
(9, 8, 15, N'TPBank', '3434343434', N'TRAN NHAT MINH', DATEADD(DAY, -2, GETDATE()), 4780000, 478000, 10, 4302000, N'One guest cannot participate.', 'Refunded', NULL, DATEADD(DAY, -1, GETDATE()), 1),
(10, 13, 10, N'Vietcombank', '5656565656', N'DO NGOC CHI', DATEADD(DAY, -1, GETDATE()), 11080000, 1108000, 10, 9972000, N'Need to reschedule the Phu Quoc tour.', 'Pending', NULL, NULL, NULL);
SET IDENTITY_INSERT CancellationRequests OFF;
GO

/* =========================================================
   5. PAYMENT DB
   ========================================================= */
USE StayHub_PaymentDb;
GO

SET IDENTITY_INSERT Transactions ON;
INSERT INTO Transactions (Id, OrderId, Amount, Provider, ProviderTxnId, Status) VALUES
(1, 1, 10680000, 'VNPay', 'VNP_202606010001', 'Success'),
(2, 2, 8580000, 'MoMo', 'MOMO_202606010002', 'Success'),
(3, 3, 1790000, 'PayOS', 'PAYOS_202606010003', 'Success'),
(4, 4, 4870000, 'VNPay', 'VNP_202606010004', 'Success'),
(5, 5, 3980000, 'VNPay', 'VNP_202606010005', 'Pending'),
(6, 6, 13480000, 'MoMo', 'MOMO_202606010006', 'Success'),
(7, 7, 990000, 'PayOS', 'PAYOS_202606010007', 'Success'),
(8, 8, 4780000, 'VNPay', 'VNP_202606010008', 'Success'),
(9, 9, 5780000, 'VNPay', 'VNP_202606010009', 'Failed'),
(10, 10, 6880000, 'MoMo', 'MOMO_202606010010', 'Success'),
(11, 11, 4990000, 'PayOS', 'PAYOS_202606010011', 'Pending'),
(12, 12, 1580000, 'VNPay', 'VNP_202606010012', 'Success'),
(13, 13, 11080000, 'MoMo', 'MOMO_202606010013', 'Success'),
(14, 14, 9180000, 'VNPay', 'VNP_202606010014', 'Pending'),
(15, 15, 3090000, 'PayOS', 'PAYOS_202606010015', 'Success');
SET IDENTITY_INSERT Transactions OFF;
GO

/* =========================================================
   6. VOUCHER DB
   ========================================================= */
USE StayHub_VoucherDb;
GO

SET IDENTITY_INSERT Vouchers ON;
INSERT INTO Vouchers (Id, Code, TourId, DiscountType, DiscountValue, UsedCount, AvailableCount, StartDate, EndDate, Description, CreatorId) VALUES
(1, 'WELCOME100', NULL, 'Amount', 100000, 25, 500, '2026-01-01', '2026-12-31', N'Discount 100k for new customers.', 1),
(2, 'PHUQUOC300', 1, 'Amount', 300000, 8, 100, '2026-05-01', '2026-08-31', N'Phu Quoc summer tour offer.', 2),
(3, 'DALAT10', 2, 'Percent', 10, 12, 80, '2026-05-01', '2026-07-31', N'10% off Da Lat tour.', 2),
(4, 'HOIAN100', 3, 'Amount', 100000, 4, 60, '2026-05-15', '2026-09-30', N'Hoi An weekend offer.', 3),
(5, 'HALONGVIP', 4, 'Amount', 500000, 6, 40, '2026-05-01', '2026-10-31', N'Discount on Ha Long cruise.', 3),
(6, 'MEKONGFUN', 5, 'Amount', 200000, 10, 120, '2026-05-01', '2026-12-31', N'Mekong Delta offer.', 2),
(7, 'SAPA15', 6, 'Percent', 15, 3, 50, '2026-06-01', '2026-09-30', N'Discount on Sapa trekking tour.', 2),
(8, 'DANANG300', 8, 'Amount', 300000, 8, 100, '2026-06-01', '2026-08-31', N'Da Nang summer combo.', 3),
(9, 'FOODIE50', 9, 'Amount', 50000, 20, 200, '2026-01-01', '2026-12-31', N'Food tour discount.', 1),
(10, 'HONEYMOON500', 10, 'Amount', 500000, 2, 30, '2026-06-01', '2026-12-31', N'Honeymoon offer.', 1),
(11, 'PHOTO99', 11, 'Amount', 99000, 6, 100, '2026-05-01', '2026-12-31', N'Photo walk offer.', 2),
(12, 'ISLAND200', 12, 'Amount', 200000, 5, 70, '2026-05-01', '2026-09-30', N'Island hopping offer.', 3);
SET IDENTITY_INSERT Vouchers OFF;
GO

SET IDENTITY_INSERT UserVouchers ON;
INSERT INTO UserVouchers (Id, UserId, VoucherId, Quantity, Status) VALUES
(1, 8, 1, 1, 'Available'), (2, 8, 2, 1, 'Used'), (3, 9, 5, 1, 'Used'),
(4, 10, 4, 1, 'Used'), (5, 11, 6, 1, 'Used'), (6, 12, 1, 1, 'Available'),
(7, 13, 10, 1, 'Used'), (8, 14, 11, 1, 'Used'), (9, 15, 12, 1, 'Used'),
(10, 16, 3, 1, 'Available'), (11, 17, 8, 1, 'Used'), (12, 9, 9, 2, 'Available'),
(13, 10, 2, 1, 'Available'), (14, 11, 7, 1, 'Available'), (15, 12, 5, 1, 'Expired');
SET IDENTITY_INSERT UserVouchers OFF;
GO

/* =========================================================
   7. SYSTEM DB
   ========================================================= */
USE StayHub_SystemDb;
GO

SET IDENTITY_INSERT Notifications ON;
INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES
(1, 8, N'Booking successful', N'Your Phu Quoc order has been confirmed.', 0, DATEADD(DAY, -24, GETDATE())),
(2, 9, N'Payment successful', N'The transaction for the Ha Long cruise has been completed.', 1, DATEADD(DAY, -22, GETDATE())),
(3, 10, N'Check-in successful', N'Your Hoi An ticket has been checked in.', 1, DATEADD(DAY, -20, GETDATE())),
(4, 11, N'Tour reminder', N'Your Mekong Delta tour is departing soon.', 0, DATEADD(DAY, -18, GETDATE())),
(5, 12, N'Tour cancellation request', N'Your tour cancellation request is pending.', 0, DATEADD(DAY, -5, GETDATE())),
(6, 13, N'Honeymoon setup', N'StayHub has noted your honeymoon setup request.', 1, DATEADD(DAY, -14, GETDATE())),
(7, 14, N'Thank you for your review', N'Your review helps the community choose better tours.', 1, DATEADD(DAY, -12, GETDATE())),
(8, 15, N'Refund successful', N'The partial refund request has been processed.', 0, DATEADD(DAY, -1, GETDATE())),
(9, 16, N'Order cancelled', N'Your Da Lat order has been cancelled.', 1, DATEADD(DAY, -8, GETDATE())),
(10, 17, N'New voucher', N'You just received the DANANG300 voucher.', 0, DATEADD(DAY, -7, GETDATE())),
(11, 4, N'Schedule assignment', N'You are assigned to the Phu Quoc tour on 01/06/2026.', 0, DATEADD(DAY, -3, GETDATE())),
(12, 5, N'Schedule assignment', N'You are assigned to assist the Ha Long tour.', 0, DATEADD(DAY, -2, GETDATE()));
SET IDENTITY_INSERT Notifications OFF;
GO

SET IDENTITY_INSERT SystemSettings ON;
INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES
(1, 'BOOKING_HOLD_MINUTES', '15', N'Seat holding time when creating an unpaid order.', GETDATE()),
(2, 'CANCELLATION_FEE_PERCENT', '10', N'Default tour cancellation fee percentage.', GETDATE()),
(3, 'MAX_TICKET_PER_ORDER', '10', N'Maximum number of tickets in an order.', GETDATE()),
(4, 'SUPPORT_EMAIL', 'support@stayhub.vn', N'Customer support email.', GETDATE()),
(5, 'SUPPORT_PHONE', '1900 6868', N'Support hotline number.', GETDATE()),
(6, 'LOCATION_LOG_INTERVAL_SECONDS', '60', N'Location sending interval during the tour.', GETDATE()),
(7, 'MOMENT_MAX_IMAGE_MB', '5', N'Maximum image size for moments.', GETDATE()),
(8, 'REVIEW_EDIT_DAYS', '7', N'Number of days allowed to edit a review.', GETDATE()),
(9, 'DEFAULT_COUNTRY', 'Vietnam', N'System default country.', GETDATE()),
(10, 'PAYMENT_PROVIDER_DEFAULT', 'VNPay', N'Default payment gateway.', GETDATE());
SET IDENTITY_INSERT SystemSettings OFF;
GO

/* =========================================================
   8. SOCIAL DB
   ========================================================= */
USE StayHub_SocialDb;
GO

SET IDENTITY_INSERT Friendships ON;
INSERT INTO Friendships (Id, RequesterId, ReceiverId, Status, CreatedAt) VALUES
(1, 8, 9, 'Accepted', DATEADD(DAY, -30, GETDATE())),
(2, 8, 10, 'Accepted', DATEADD(DAY, -29, GETDATE())),
(3, 9, 11, 'Pending', DATEADD(DAY, -20, GETDATE())),
(4, 10, 12, 'Accepted', DATEADD(DAY, -19, GETDATE())),
(5, 11, 13, 'Declined', DATEADD(DAY, -18, GETDATE())),
(6, 12, 14, 'Accepted', DATEADD(DAY, -17, GETDATE())),
(7, 13, 15, 'Pending', DATEADD(DAY, -16, GETDATE())),
(8, 14, 16, 'Accepted', DATEADD(DAY, -15, GETDATE())),
(9, 15, 17, 'Accepted', DATEADD(DAY, -14, GETDATE())),
(10, 16, 8, 'Pending', DATEADD(DAY, -13, GETDATE())),
(11, 17, 9, 'Accepted', DATEADD(DAY, -12, GETDATE())),
(12, 13, 8, 'Accepted', DATEADD(DAY, -11, GETDATE()));
SET IDENTITY_INSERT Friendships OFF;
GO

SET IDENTITY_INSERT ChatRooms ON;
INSERT INTO ChatRooms (Id, ScheduleId, RoomName, IsGroupChat, CreatedAt) VALUES
(1, 1, N'Phu Quoc 01/06 - Group', 1, DATEADD(DAY, -25, GETDATE())),
(2, 6, N'Ha Long 10/06 - Group', 1, DATEADD(DAY, -22, GETDATE())),
(3, 5, N'Hoi An 20/06 - Group', 1, DATEADD(DAY, -20, GETDATE())),
(4, 7, N'Mekong 12/06 - Group', 1, DATEADD(DAY, -18, GETDATE())),
(5, 12, N'Cam Ranh Honeymoon', 1, DATEADD(DAY, -15, GETDATE())),
(6, NULL, N'An and Bao', 0, DATEADD(DAY, -14, GETDATE())),
(7, NULL, N'Chi and Giang', 0, DATEADD(DAY, -13, GETDATE())),
(8, 10, N'Da Nang 10/07 - Group', 1, DATEADD(DAY, -8, GETDATE())),
(9, 14, N'Nha Trang 28/06 - Group', 1, DATEADD(DAY, -7, GETDATE())),
(10, 8, N'Sapa 01/07 - Group', 1, DATEADD(DAY, -6, GETDATE()));
SET IDENTITY_INSERT ChatRooms OFF;
GO

SET IDENTITY_INSERT ChatMembers ON;
INSERT INTO ChatMembers (Id, ChatRoomId, UserId, JoinedAt) VALUES
(1, 1, 8, DATEADD(DAY, -25, GETDATE())), (2, 1, 4, DATEADD(DAY, -25, GETDATE())), (3, 1, 5, DATEADD(DAY, -25, GETDATE())),
(4, 2, 9, DATEADD(DAY, -22, GETDATE())), (5, 2, 6, DATEADD(DAY, -22, GETDATE())),
(6, 3, 10, DATEADD(DAY, -20, GETDATE())), (7, 3, 5, DATEADD(DAY, -20, GETDATE())),
(8, 4, 11, DATEADD(DAY, -18, GETDATE())), (9, 4, 4, DATEADD(DAY, -18, GETDATE())),
(10, 5, 13, DATEADD(DAY, -15, GETDATE())), (11, 5, 5, DATEADD(DAY, -15, GETDATE())),
(12, 6, 8, DATEADD(DAY, -14, GETDATE())), (13, 6, 9, DATEADD(DAY, -14, GETDATE())),
(14, 7, 10, DATEADD(DAY, -13, GETDATE())), (15, 7, 12, DATEADD(DAY, -13, GETDATE())),
(16, 8, 17, DATEADD(DAY, -8, GETDATE())), (17, 8, 6, DATEADD(DAY, -8, GETDATE())),
(18, 9, 15, DATEADD(DAY, -7, GETDATE())), (19, 10, 8, DATEADD(DAY, -6, GETDATE())), (20, 10, 7, DATEADD(DAY, -6, GETDATE()));
SET IDENTITY_INSERT ChatMembers OFF;
GO

SET IDENTITY_INSERT ChatMessages ON;
INSERT INTO ChatMessages (Id, ChatRoomId, SenderId, Content, IsRead, SentAt) VALUES
(1, 1, 4, N'Hello everyone, the Phu Quoc tour will gather at the airport at 8 AM.', 1, DATEADD(DAY, -24, GETDATE())),
(2, 1, 8, N'Got the info, thanks.', 1, DATEADD(DAY, -24, GETDATE())),
(3, 2, 6, N'Everyone please prepare your ID for cruise check-in.', 1, DATEADD(DAY, -21, GETDATE())),
(4, 2, 9, N'Do we need to bring swimwear for the tour?', 0, DATEADD(DAY, -21, GETDATE())),
(5, 3, 5, N'Tonight we will go to the ancient town and release lanterns.', 1, DATEADD(DAY, -19, GETDATE())),
(6, 4, 4, N'The floating market trip is early, so everyone sleep early.', 1, DATEADD(DAY, -17, GETDATE())),
(7, 5, 13, N'Can we set up a small cake?', 0, DATEADD(DAY, -14, GETDATE())),
(8, 5, 5, N'Yes sir, we have noted that down.', 0, DATEADD(DAY, -14, GETDATE())),
(9, 6, 8, N'Bao, are you going to Phu Quoc with us?', 1, DATEADD(DAY, -13, GETDATE())),
(10, 6, 9, N'Let me check my schedule first.', 0, DATEADD(DAY, -13, GETDATE())),
(11, 8, 6, N'Da Nang in July is quite sunny, prepare sunscreen everyone.', 0, DATEADD(DAY, -7, GETDATE())),
(12, 10, 7, N'Sapa trekking requires shoes with good grip, everyone.', 0, DATEADD(DAY, -5, GETDATE()));
SET IDENTITY_INSERT ChatMessages OFF;
GO

SET IDENTITY_INSERT TourMoments ON;
INSERT INTO TourMoments (Id, ScheduleId, UserId, ImageUrl, Caption, Lat, Lng, CreatedAt) VALUES
(1, 1, 8, 'https://cdn.stayhub.vn/moments/phu-quoc-1.jpg', N'Phu Quoc sea is incredibly blue!', 10.0588, 104.0355, DATEADD(DAY, -23, GETDATE())),
(2, 6, 9, 'https://cdn.stayhub.vn/moments/ha-long-1.jpg', N'Morning on the Ha Long cruise.', 20.9101, 107.1839, DATEADD(DAY, -21, GETDATE())),
(3, 5, 10, 'https://cdn.stayhub.vn/moments/hoi-an-1.jpg', N'Hoi An is so beautiful when lit up.', 15.8801, 108.3380, DATEADD(DAY, -19, GETDATE())),
(4, 7, 11, 'https://cdn.stayhub.vn/moments/mekong-1.jpg', N'Breakfast on the floating market.', 10.0035, 105.7823, DATEADD(DAY, -17, GETDATE())),
(5, 12, 13, 'https://cdn.stayhub.vn/moments/cam-ranh-1.jpg', N'Resort view is totally worth it.', 11.8462, 109.2221, DATEADD(DAY, -14, GETDATE())),
(6, 13, 14, 'https://cdn.stayhub.vn/moments/ha-noi-1.jpg', N'Old Quarter through a film lens.', 21.0333, 105.8500, DATEADD(DAY, -12, GETDATE())),
(7, 14, 15, 'https://cdn.stayhub.vn/moments/nha-trang-1.jpg', N'Crystal clear Nha Trang beach.', 12.2388, 109.1967, DATEADD(DAY, -10, GETDATE())),
(8, 10, 17, 'https://cdn.stayhub.vn/moments/da-nang-1.jpg', N'Dragon Bridge at night.', 16.0610, 108.2276, DATEADD(DAY, -8, GETDATE())),
(9, 8, 8, 'https://cdn.stayhub.vn/moments/sapa-1.jpg', N'Sapa clouds embracing the mountains.', 22.3264, 103.8391, DATEADD(DAY, -6, GETDATE())),
(10, 9, 12, 'https://cdn.stayhub.vn/moments/ninh-binh-1.jpg', N'Peaceful Trang An.', 20.2538, 105.9190, DATEADD(DAY, -5, GETDATE()));
SET IDENTITY_INSERT TourMoments OFF;
GO

SET IDENTITY_INSERT MomentReactions ON;
INSERT INTO MomentReactions (Id, MomentId, UserId, IsLike) VALUES
(1, 1, 9, 1), (2, 1, 10, 1), (3, 2, 8, 1), (4, 2, 11, 1),
(5, 3, 12, 1), (6, 3, 14, 1), (7, 4, 10, 1), (8, 4, 15, 1),
(9, 5, 17, 1), (10, 6, 8, 1), (11, 7, 13, 1), (12, 8, 14, 1),
(13, 9, 16, 1), (14, 10, 11, 1), (15, 10, 17, 1);
SET IDENTITY_INSERT MomentReactions OFF;
GO

SET IDENTITY_INSERT MomentComments ON;
INSERT INTO MomentComments (Id, MomentId, UserId, Comment, Timestamp) VALUES
(1, 1, 9, N'So beautiful, I want to go too!', DATEADD(DAY, -22, GETDATE())),
(2, 1, 10, N'The sea color is so premium.', DATEADD(DAY, -22, GETDATE())),
(3, 2, 8, N'The cruise looks so fancy.', DATEADD(DAY, -20, GETDATE())),
(4, 3, 12, N'Hoi An has exactly the right vibe.', DATEADD(DAY, -18, GETDATE())),
(5, 4, 15, N'The floating market looks fun.', DATEADD(DAY, -16, GETDATE())),
(6, 5, 17, N'What is the name of this resort?', DATEADD(DAY, -13, GETDATE())),
(7, 6, 8, N'Looks like a postcard.', DATEADD(DAY, -11, GETDATE())),
(8, 7, 13, N'Nha Trang in summer is the best.', DATEADD(DAY, -9, GETDATE())),
(9, 8, 14, N'Dragon Bridge is super bright.', DATEADD(DAY, -7, GETDATE())),
(10, 9, 16, N'Is Sapa cold this season?', DATEADD(DAY, -5, GETDATE())),
(11, 10, 11, N'Trang An looks so chill.', DATEADD(DAY, -4, GETDATE())),
(12, 10, 17, N'This shot is stunning.', DATEADD(DAY, -4, GETDATE()));
SET IDENTITY_INSERT MomentComments OFF;
GO

SET IDENTITY_INSERT LocationLogs ON;
INSERT INTO LocationLogs (Id, ScheduleId, UserId, Lat, Lng, Timestamp) VALUES
(1, 1, 8, 10.0588, 104.0355, DATEADD(MINUTE, -120, GETDATE())),
(2, 1, 4, 10.0590, 104.0358, DATEADD(MINUTE, -118, GETDATE())),
(3, 6, 9, 20.9101, 107.1839, DATEADD(MINUTE, -110, GETDATE())),
(4, 6, 6, 20.9105, 107.1841, DATEADD(MINUTE, -108, GETDATE())),
(5, 5, 10, 15.8801, 108.3380, DATEADD(MINUTE, -100, GETDATE())),
(6, 7, 11, 10.0035, 105.7823, DATEADD(MINUTE, -95, GETDATE())),
(7, 12, 13, 11.8462, 109.2221, DATEADD(MINUTE, -90, GETDATE())),
(8, 13, 14, 21.0333, 105.8500, DATEADD(MINUTE, -80, GETDATE())),
(9, 14, 15, 12.2388, 109.1967, DATEADD(MINUTE, -70, GETDATE())),
(10, 10, 17, 16.0610, 108.2276, DATEADD(MINUTE, -60, GETDATE())),
(11, 8, 8, 22.3264, 103.8391, DATEADD(MINUTE, -50, GETDATE())),
(12, 9, 12, 20.2538, 105.9190, DATEADD(MINUTE, -40, GETDATE()));
SET IDENTITY_INSERT LocationLogs OFF;
GO

/* =========================================================
   9. AI DB
   ========================================================= */
USE StayHub_AiDb;
GO

SET IDENTITY_INSERT UserPreferences ON;
INSERT INTO UserPreferences (Id, UserId, InterestTags, MinBudget, MaxBudget, UpdatedAt) VALUES
(1, 8, N'beach,island,photography,food', 1500000, 6000000, DATEADD(DAY, -10, GETDATE())),
(2, 9, N'luxury,cruise,heritage', 3000000, 9000000, DATEADD(DAY, -9, GETDATE())),
(3, 10, N'culture,heritage,local-food', 1000000, 4000000, DATEADD(DAY, -8, GETDATE())),
(4, 11, N'family,eco,mekong', 1000000, 5000000, DATEADD(DAY, -7, GETDATE())),
(5, 12, N'nature,heritage,short-trip', 1200000, 4500000, DATEADD(DAY, -6, GETDATE())),
(6, 13, N'honeymoon,luxury,wellness', 5000000, 15000000, DATEADD(DAY, -5, GETDATE())),
(7, 14, N'photography,city,food', 500000, 3000000, DATEADD(DAY, -4, GETDATE())),
(8, 15, N'island,seafood,beach', 1500000, 5000000, DATEADD(DAY, -3, GETDATE())),
(9, 16, N'mountain,chill,coffee', 1000000, 4000000, DATEADD(DAY, -2, GETDATE())),
(10, 17, N'city-break,theme-park,beach', 2000000, 6000000, DATEADD(DAY, -1, GETDATE()));
SET IDENTITY_INSERT UserPreferences OFF;
GO

SET IDENTITY_INSERT AILogs ON;
INSERT INTO AILogs (Id, UserId, Budget, Days, ResultIds, CreatedAt) VALUES
(1, 8, 6000000, 4, '1,12,2', DATEADD(DAY, -10, GETDATE())),
(2, 9, 9000000, 2, '4,10,1', DATEADD(DAY, -9, GETDATE())),
(3, 10, 3000000, 2, '3,7,11', DATEADD(DAY, -8, GETDATE())),
(4, 11, 5000000, 2, '5,7,8', DATEADD(DAY, -7, GETDATE())),
(5, 12, 4000000, 2, '7,3,2', DATEADD(DAY, -6, GETDATE())),
(6, 13, 12000000, 3, '10,4,1', DATEADD(DAY, -5, GETDATE())),
(7, 14, 2500000, 1, '11,9,3', DATEADD(DAY, -4, GETDATE())),
(8, 15, 5000000, 2, '12,1,8', DATEADD(DAY, -3, GETDATE())),
(9, 16, 4000000, 3, '2,6,7', DATEADD(DAY, -2, GETDATE())),
(10, 17, 6000000, 3, '8,3,12', DATEADD(DAY, -1, GETDATE()));
SET IDENTITY_INSERT AILogs OFF;
GO

USE master;
GO
PRINT '=======================================================';
PRINT 'INSERT SAMPLE DATA CHO STAYHUB THANH CONG!';
PRINT '=======================================================';