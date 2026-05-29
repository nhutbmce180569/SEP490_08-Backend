USE master;
GO

-- =========================================================================
-- PHẦN 1: XÓA SẠCH DATABASE NẾU ĐÃ TỒN TẠI (RESET)
-- Sử dụng ROLLBACK IMMEDIATE để ép ngắt mọi kết nối đang dùng Database
-- =========================================================================

DECLARE @Databases TABLE (DbName VARCHAR(50));
INSERT INTO @Databases VALUES 
('StayHub_ContentDb'), ('StayHub_IdentityDb'), ('StayHub_CatalogDb'), 
('StayHub_BookingDb'), ('StayHub_PaymentDb'), ('StayHub_VoucherDb'), 
('StayHub_SystemDb'), ('StayHub_SocialDb'), ('StayHub_AiDb'),
('StayHub_TourCatalogDb'); -- Xóa luôn tên db cũ nếu còn kẹt lại

DECLARE @DbName VARCHAR(50);
DECLARE @Sql NVARCHAR(MAX);

DECLARE db_cursor CURSOR FOR SELECT DbName FROM @Databases;
OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @DbName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF DB_ID(@DbName) IS NOT NULL
    BEGIN
        SET @Sql = 'ALTER DATABASE [' + @DbName + '] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [' + @DbName + '];';
        EXEC sp_executesql @Sql;
        PRINT 'Đã xóa database: ' + @DbName;
    END
    FETCH NEXT FROM db_cursor INTO @DbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;
GO

-- =========================================================================
-- PHẦN 2: KHỞI TẠO LẠI 9 DATABASES THEO CHUẨN MICROSERVICES MỚI
-- =========================================================================

-- -------------------------------------------------------------------------
-- 1. CONTENT API (Master Data)
-- -------------------------------------------------------------------------
CREATE DATABASE StayHub_ContentDb;
GO
USE StayHub_ContentDb;
GO

CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Slug VARCHAR(255) UNIQUE NOT NULL,
    IconUrl NVARCHAR(MAX),
    Description NVARCHAR(MAX),
    IsActive BIT DEFAULT 1
);

CREATE TABLE Banners (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(255) NOT NULL,
    ImageUrl NVARCHAR(MAX) NOT NULL,
    TargetUrl NVARCHAR(MAX),
    Priority INT DEFAULT 0,
    IsActive BIT DEFAULT 1
);

CREATE TABLE TourismInformation (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(255) NOT NULL,
    Type VARCHAR(50) NOT NULL, -- Destination, Heritage, LocalFood, Restaurant, Activity, Other
    Description NVARCHAR(MAX),
    Address NVARCHAR(255),
    City NVARCHAR(100),
    Country NVARCHAR(100) DEFAULT N'Vietnam',
    Latitude FLOAT,
    Longitude FLOAT,
    ImageUrl NVARCHAR(MAX),
    SourceName NVARCHAR(255),
    SourceUrl NVARCHAR(MAX),
    Status VARCHAR(50) DEFAULT 'Active',
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE TicketTypes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL
);

GO

-- -------------------------------------------------------------------------
-- 2. IDENTITY API
-- -------------------------------------------------------------------------
CREATE DATABASE StayHub_IdentityDb;
GO
USE StayHub_IdentityDb;
GO

-- Bảng Quản lý Phân quyền (Roles)
CREATE TABLE Roles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name VARCHAR(50) UNIQUE NOT NULL, -- Admin, TourManager, Staff, Customer
    Description NVARCHAR(255)
);

-- Bảng người dùng (Đã gỡ bỏ RoleId)
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FullName NVARCHAR(255) NOT NULL,
	AvatarUrl NVARCHAR(MAX),
    Provider VARCHAR(50) DEFAULT 'Local',
    PhoneNumber VARCHAR(20),
    Gender VARCHAR(20),
    DateOfBirth DATE,
    Status VARCHAR(50) DEFAULT 'Active',
    LocPrivacy BIT DEFAULT 1,
    MomentPrivacy BIT DEFAULT 1,
    LastOnline DATETIME,
    -- BẢO MẬT: SecurityStamp thay đổi mỗi khi đổi pass/info quan trọng
    SecurityStamp NVARCHAR(MAX) DEFAULT NEWID(), 
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE() 
);

-- Bảng trung gian Nhiều - Nhiều (1 User có nhiều Role)
CREATE TABLE UserRoles (
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    RoleId INT NOT NULL FOREIGN KEY REFERENCES Roles(Id),
    PRIMARY KEY (UserId, RoleId) -- Khóa chính kép (Composite Key)
);

CREATE TABLE RefreshTokens (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Token VARCHAR(MAX) NOT NULL,
    Expires DATETIME2 NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedByIp VARCHAR(50),
    RevokedAt DATETIME2 NULL,
    RevokedByIp VARCHAR(50),
    ReplacedByToken VARCHAR(MAX) NULL,
    ReasonRevoked NVARCHAR(MAX),
    -- Logic kiểm tra nhanh
    IsExpired AS (CASE WHEN GETDATE() >= Expires THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END),
    IsActive AS (CASE WHEN RevokedAt IS NULL AND GETDATE() < Expires THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END)
);

GO


-- -------------------------------------------------------------------------
-- 3. CATALOG API
-- -------------------------------------------------------------------------
CREATE DATABASE StayHub_CatalogDb;
GO
USE StayHub_CatalogDb;
GO

CREATE TABLE Tours (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    
    CategoryId INT NOT NULL,
    Name NVARCHAR(255) NOT NULL,
    Description NVARCHAR(MAX),

    Country NVARCHAR(100),
    City NVARCHAR(100),
    Address NVARCHAR(255),

    ImageUrl NVARCHAR(500),
	CreatedBy INT NOT NULL,
	UpdatedBy INT NULL,
	CreatedAt DATETIME DEFAULT GETDATE(),
	UpdatedAt DATETIME NULL,
    Status VARCHAR(50) DEFAULT 'Active'
);

CREATE TABLE TourItineraries (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TourId INT NOT NULL FOREIGN KEY REFERENCES Tours(Id),
    DayNumber INT NOT NULL,
    Title NVARCHAR(255),
    Description NVARCHAR(MAX),

    StartDuration TIME,
    EndDuration TIME,

    LocationName NVARCHAR(255),
    LocationLat FLOAT,
    LocationLng FLOAT,
    TourismInfoId INT NULL -- Logical FK -> ContentDb.TourismInformation
);

CREATE TABLE TourSchedules (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    TourId INT NOT NULL FOREIGN KEY REFERENCES Tours(Id),
    DepartureDate DATETIME2 NOT NULL,
    ReturnDate DATETIME2 NOT NULL,
    Note NVARCHAR(MAX)
);
CREATE TABLE TourScheduleItineraries (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ScheduleId INT NOT NULL 
        FOREIGN KEY REFERENCES TourSchedules(Id),
    DayNumber INT NOT NULL,
    ItineraryDate DATETIME NOT NULL, -- Ngày thực tế diễn ra (VD: 2026-05-15)
    
    Title NVARCHAR(255),
    Description NVARCHAR(MAX),
    StartDuration TIME,
    EndDuration TIME,

    LocationName NVARCHAR(255),
    LocationLat FLOAT,
    LocationLng FLOAT,
    TourismInfoId INT NULL -- Logical FK -> ContentDb.TourismInformation
);
CREATE TABLE TourScheduleStaffs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ScheduleId INT NOT NULL FOREIGN KEY REFERENCES TourSchedules(Id),
    StaffId INT NOT NULL, -- Logical FK -> IdentityDb.Users (Role: Staff)
    AssignedRole NVARCHAR(255) -- Vai trò: Hướng dẫn viên, Tài xế, Hậu cần...
);
CREATE TABLE TourScheduleTickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ScheduleId INT NOT NULL, -- Logical/FK -> TourSchedules.Id
    TicketTypeId INT NOT NULL, -- FK -> TicketTypes.Id
    Price BIGINT NOT NULL,
    Quantity INT NOT NULL,
    SoldQuantity INT DEFAULT 0,
    AvailableQuantity INT NOT NULL,
    IsActive BIT DEFAULT 1,
    Note NVARCHAR(MAX),

    FOREIGN KEY (ScheduleId) REFERENCES TourSchedules(Id)
);
CREATE TABLE Wishlists (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    TourId INT NOT NULL FOREIGN KEY REFERENCES Tours(Id)
);

CREATE TABLE Reviews (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    TourId INT NOT NULL FOREIGN KEY REFERENCES Tours(Id),
    Rating INT CHECK (Rating >= 1 AND Rating <= 5),
    Comment NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL

);
CREATE TABLE ReviewReplies (
    Id INT IDENTITY(1,1) PRIMARY KEY,

    ReviewId INT NOT NULL 
        FOREIGN KEY REFERENCES Reviews(Id),

    UserId INT NOT NULL, 
    -- Logical FK -> IdentityDb.Users
    -- Có thể là staff/operator/customer tùy business

    Content NVARCHAR(MAX) NOT NULL,

    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);
GO

-- -------------------------------------------------------------------------
-- 4. BOOKING API
-- -------------------------------------------------------------------------
CREATE DATABASE StayHub_BookingDb;
GO

USE StayHub_BookingDb;
GO

-- =========================
-- ORDERS
-- =========================
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    
    CustomerId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    ScheduleId INT NOT NULL, -- Logical FK -> CatalogDb.TourSchedules

    TicketCount INT NOT NULL,

    DiscountValue BIGINT DEFAULT 0,
    FinalAmount BIGINT NOT NULL,

    Note NVARCHAR(MAX),

    Status VARCHAR(50) DEFAULT 'Pending',
    -- Pending, Paid, Cancelled, Completed

    OrderedAt DATETIME DEFAULT GETDATE(),

    InviteToken VARCHAR(255) UNIQUE
);
GO

-- =========================
-- TICKETS
-- =========================
CREATE TABLE Tickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,

    OrderId INT NOT NULL,
    UserId INT NULL, -- Logical FK -> IdentityDb.Users

    AttendeeName NVARCHAR(255) NOT NULL,
    IdCard VARCHAR(50) NOT NULL,

    DateOfBirth DATE,
    Gender VARCHAR(20),
    Nationality NVARCHAR(100),

    QrCode VARCHAR(255) UNIQUE,

    CheckInStatus VARCHAR(50) DEFAULT 'Pending',
    -- Pending, CheckedIn

    CONSTRAINT FK_Tickets_Orders
        FOREIGN KEY (OrderId)
        REFERENCES Orders(Id)
);
GO

-- =========================
-- CANCELLATION REQUESTS
-- =========================
CREATE TABLE CancellationRequests (
    Id INT IDENTITY(1,1) PRIMARY KEY,

    OrderId INT NOT NULL,
    CustomerId INT NOT NULL, -- Logical FK -> IdentityDb.Users

    BankName NVARCHAR(255) NOT NULL,
    AccountNumber VARCHAR(100) NOT NULL,
    AccountHolderName NVARCHAR(255) NOT NULL,

    RequestedAt DATETIME DEFAULT GETDATE(),

    OriginalAmount BIGINT NOT NULL,
    CancellationFee BIGINT NOT NULL,
    FeePercent INT NOT NULL,
    RefundAmount BIGINT NOT NULL,

    Reason NVARCHAR(MAX) NOT NULL,

    Status VARCHAR(50) NOT NULL,
    -- Pending, Approved, Rejected, Refunded

    RejectReason NVARCHAR(MAX) NULL, 

    ProcessedAt DATETIME NULL,
    ProcessedBy INT NULL, -- Logical FK -> IdentityDb.Users

    CONSTRAINT FK_CancellationRequests_Orders
        FOREIGN KEY (OrderId)
        REFERENCES Orders(Id)
);
GO

-- -------------------------------------------------------------------------
-- 5. PAYMENT API
-- -------------------------------------------------------------------------
CREATE DATABASE StayHub_PaymentDb;
GO
USE StayHub_PaymentDb;
GO

CREATE TABLE Transactions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL, -- Logical FK -> BookingDb.Orders
    Amount BIGINT NOT NULL, -- CHUYỂN SANG BIGINT
    Provider VARCHAR(50) NOT NULL, -- MoMo, VNPay...
    ProviderTxnId VARCHAR(255),
    Status VARCHAR(50) DEFAULT 'Pending' -- Pending, Success, Failed
);
GO

-- -------------------------------------------------------------------------
-- 6. VOUCHER API
-- -------------------------------------------------------------------------
CREATE DATABASE StayHub_VoucherDb;
GO
USE StayHub_VoucherDb;
GO

CREATE TABLE Vouchers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Code VARCHAR(50) UNIQUE NOT NULL,
    TourId INT NULL, -- Logical FK -> CatalogDb.Tours (Null = All Tours)
    DiscountType VARCHAR(50) NOT NULL, -- Percent, Amount
    DiscountValue BIGINT NOT NULL, -- CHUYỂN SANG BIGINT (Giữ % dưới dạng số nguyên hoặc số tiền trực tiếp)
    UsedCount INT DEFAULT 0,
    AvailableCount INT NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    Description NVARCHAR(MAX),
    CreatorId INT NOT NULL -- Logical FK -> IdentityDb.Users (System Admin / Operator)
);

CREATE TABLE UserVouchers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    VoucherId INT NOT NULL FOREIGN KEY REFERENCES Vouchers(Id),
    Quantity INT DEFAULT 1,
    Status VARCHAR(50) DEFAULT 'Available' -- Available, Used, Expired
);
GO

-- -------------------------------------------------------------------------
-- 7. SYSTEM API (Moderation & Communication)
-- -------------------------------------------------------------------------
CREATE DATABASE StayHub_SystemDb;
GO
USE StayHub_SystemDb;
GO

CREATE TABLE Notifications (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    Title NVARCHAR(255) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE SystemSettings (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SettingKey VARCHAR(100) UNIQUE NOT NULL,
    SettingValue NVARCHAR(MAX) NOT NULL,
    Description NVARCHAR(MAX),
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);
GO

-- -------------------------------------------------------------------------
-- 8. SOCIAL API (Map, Moments, Friends, Chat)
-- -------------------------------------------------------------------------
CREATE DATABASE StayHub_SocialDb;
GO
USE StayHub_SocialDb;
GO

CREATE TABLE Friendships (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RequesterId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    ReceiverId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    Status VARCHAR(50) DEFAULT 'Pending', -- Pending, Accepted, Declined
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE ChatRooms (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ScheduleId INT NULL, -- Logical FK -> CatalogDb.TourSchedules
    RoomName NVARCHAR(255),
    IsGroupChat BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE ChatMembers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ChatRoomId INT NOT NULL FOREIGN KEY REFERENCES ChatRooms(Id),
    UserId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    JoinedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE ChatMessages (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ChatRoomId INT NOT NULL FOREIGN KEY REFERENCES ChatRooms(Id),
    SenderId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    Content NVARCHAR(MAX) NOT NULL,
    IsRead BIT DEFAULT 0,
    SentAt DATETIME2 DEFAULT GETDATE()
);


CREATE TABLE TourMoments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ScheduleId INT NOT NULL, -- Logical FK -> CatalogDb.TourSchedules
    UserId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    ImageUrl NVARCHAR(MAX) NOT NULL,
    Caption NVARCHAR(MAX),
    Lat FLOAT,
    Lng FLOAT,
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE MomentReactions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MomentId INT NOT NULL FOREIGN KEY REFERENCES TourMoments(Id),
    UserId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    IsLike BIT DEFAULT 1
);

CREATE TABLE MomentComments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    MomentId INT NOT NULL FOREIGN KEY REFERENCES TourMoments(Id),
    UserId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    Comment NVARCHAR(MAX) NOT NULL,
    Timestamp DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE LocationLogs (
    Id BIGINT IDENTITY(1,1) PRIMARY KEY, -- Sinh ra nhiều log nên dùng BigInt
    ScheduleId INT NOT NULL, -- Logical FK -> CatalogDb.TourSchedules
    UserId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    Lat FLOAT NOT NULL,
    Lng FLOAT NOT NULL,
    Timestamp DATETIME2 DEFAULT GETDATE()
);

GO

-- -------------------------------------------------------------------------
-- 9. AI API
-- -------------------------------------------------------------------------
CREATE DATABASE StayHub_AiDb;
GO
USE StayHub_AiDb;
GO

CREATE TABLE UserPreferences (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    InterestTags NVARCHAR(MAX),
    MinBudget BIGINT, -- CHUYỂN SANG BIGINT
    MaxBudget BIGINT, -- CHUYỂN SANG BIGINT
    UpdatedAt DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE AILogs (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    Budget BIGINT, -- CHUYỂN SANG BIGINT
    Days INT,
    ResultIds VARCHAR(MAX), -- Lưu danh sách ID Tour dạng chuỗi "1,5,12"
    CreatedAt DATETIME2 DEFAULT GETDATE()
);
GO

-- Xong! Trả về database Master để hoàn tất.
USE master;
GO
PRINT '=======================================================';
PRINT 'REBUILD TOÀN BỘ 9 DATABASES STAYHUB THEO HƯỚNG MỚI THÀNH CÔNG!';
PRINT '=======================================================';