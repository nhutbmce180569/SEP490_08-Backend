/* =========================================================
   STAYHUB DATABASE SCHEMA — single setup script (Part 1 of 2)
   Run BEFORE StayHub_SampleData_Insert.sql.

   Creates 9 microservice databases including StayHub_AiDb with:
   UserTourInteractions, TourRelevanceJudgments, UserStudyAssignments/Responses.
   All former patch/seed SQL files are merged into the sample data script.
   ========================================================= */

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
    RequirePasswordChange BIT NOT NULL DEFAULT 0,
    LocPrivacy BIT DEFAULT 1,
    MomentPrivacy BIT DEFAULT 1,
    LastOnline DATETIME,
    -- BẢO MẬT: SecurityStamp thay đổi mỗi khi đổi pass/info quan trọng
    SecurityStamp NVARCHAR(MAX) DEFAULT NEWID(), 
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    UpdatedAt DATETIME2 DEFAULT GETDATE(),
    FcmToken VARCHAR(MAX) NULL
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
    SourceName NVARCHAR(255) NOT NULL DEFAULT N'Vietnam National Administration of Tourism',
    SourceUrl NVARCHAR(500) NULL,
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

CREATE TABLE Promotions
(
    Id INT IDENTITY(1,1) PRIMARY KEY,

    Code NVARCHAR(50) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,

    -- Percentage, FixedAmount
    DiscountType NVARCHAR(50) NOT NULL,

    DiscountValue DECIMAL(18,2) NOT NULL,
    MaxDiscountAmount DECIMAL(18,2) NULL,

    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,

    Status NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

CREATE TABLE PromotionTickets
(
    PromotionId INT NOT NULL,
    TourScheduleTicketId INT NOT NULL,

    PRIMARY KEY (PromotionId, TourScheduleTicketId),

    FOREIGN KEY (PromotionId)
        REFERENCES Promotions(Id),

    FOREIGN KEY (TourScheduleTicketId)
        REFERENCES TourScheduleTickets(Id)
);
GO

CREATE TABLE Wishlists (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    TourId INT NOT NULL FOREIGN KEY REFERENCES Tours(Id),
    CONSTRAINT UQ_Wishlists_Customer_Tour UNIQUE (CustomerId, TourId)
);

CREATE TABLE Reviews (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL, -- Logical FK -> IdentityDb.Users
    TourId INT NOT NULL FOREIGN KEY REFERENCES Tours(Id),
    Rating INT CHECK (Rating >= 1 AND Rating <= 5),
    Comment NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsHidden BIT NOT NULL DEFAULT 0
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

    TotalQuantity INT NOT NULL,

    DiscountValue BIGINT DEFAULT 0,
    VoucherCode VARCHAR(50) NULL,
    TotalAmount BIGINT NOT NULL,
    FinalAmount BIGINT NOT NULL,

    Note NVARCHAR(MAX),

    Status VARCHAR(50) DEFAULT 'Pending',
    -- Pending, Paid, Cancelled, Completed

    OrderedAt DATETIME DEFAULT GETDATE(),

    InviteToken VARCHAR(255) UNIQUE
);
GO

CREATE TABLE OrderDetails (
    Id INT IDENTITY(1,1) PRIMARY KEY,

    OrderId INT NOT NULL,
    TicketTypeId INT NOT NULL, -- Logical FK -> ContentDb.TicketTypes
    TourScheduleTicketId INT NOT NULL, -- Logical FK -> CatalogDb.TourScheduleTickets

    Quantity INT NOT NULL,
    UnitPrice BIGINT NOT NULL,
    TotalPrice BIGINT NOT NULL,

    CONSTRAINT FK_OrderDetails_Orders
        FOREIGN KEY (OrderId)
        REFERENCES Orders(Id)
);
GO

CREATE TABLE Tickets (
    Id INT IDENTITY(1,1) PRIMARY KEY,

    OrderDetailId INT NOT NULL,

    UserId INT NULL, -- Logical FK -> IdentityDb.Users
    TicketTypeId INT NOT NULL, -- Logical FK -> ContentDb.TicketTypes

    AttendeeName NVARCHAR(255) NOT NULL,
    IdCard VARCHAR(50) NOT NULL,

    DateOfBirth DATE,
    Gender VARCHAR(20),
    Nationality NVARCHAR(100),

    QrCode VARCHAR(255) UNIQUE,

    CheckInStatus VARCHAR(50) DEFAULT 'Pending',
    -- Pending, CheckedIn

    CONSTRAINT FK_Tickets_OrderDetails
        FOREIGN KEY (OrderDetailId)
        REFERENCES OrderDetails(Id)
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
    MaxDiscountAmount BIGINT NULL, -- Chỉ áp dụng khi DiscountType = Percent (trần tiền giảm tối đa)
    UsedCount INT DEFAULT 0,
    AvailableCount INT NOT NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    Description NVARCHAR(MAX),
    CreatorId INT NOT NULL, -- Logical FK -> IdentityDb.Users (System Admin / Operator)
    IsActive BIT DEFAULT 1
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
    JoinedAt DATETIME2 DEFAULT GETDATE(),
    LastReadAt DATETIME2 NULL,
    IsPinned BIT DEFAULT 0,
    IsMuted BIT DEFAULT 0
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
    Privacy VARCHAR(20) NOT NULL DEFAULT 'Public' CONSTRAINT CHK_MomentPrivacy CHECK (Privacy IN ('Public', 'Private', 'Friend')),
    Status VARCHAR(20) NOT NULL DEFAULT 'Approved' CONSTRAINT CHK_TourMoments_Status CHECK (Status IN ('Approved', 'Pending', 'Flagged', 'Rejected')),
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
    Status VARCHAR(20) NOT NULL DEFAULT 'Approved' CONSTRAINT CHK_MomentComments_Status CHECK (Status IN ('Approved', 'Pending', 'Flagged', 'Rejected')),
    Timestamp DATETIME2 DEFAULT GETDATE()
);

CREATE TABLE ContentReports (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ReporterId INT NOT NULL, -- Người báo cáo
    ContentType VARCHAR(20) NOT NULL, -- 'Moment' hoặc 'Comment'
    TargetId INT NOT NULL, -- ID của Moment hoặc Comment bị báo cáo
    Reason NVARCHAR(255) NOT NULL, -- Lý do (Spam, Bạo lực, Ngôn từ kích động...)
    Details NVARCHAR(500) NULL,
    Status VARCHAR(20) DEFAULT 'Pending', -- Pending, Resolved, Dismissed
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ResolvedBy INT NULL, -- Staff/Manager xử lý duyệt
    ResolvedAt DATETIME2 NULL
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

-- Bảng ghi tín hiệu tương tác để huấn luyện / cá nhân hóa gợi ý tour (ML.NET)
CREATE TABLE UserTourInteractions (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NULL, -- Logical FK -> IdentityDb.Users (NULL = anonymous session)
    TourId INT NOT NULL, -- Logical FK -> CatalogDb.Tours
    InteractionType VARCHAR(50) NOT NULL, -- view | click | wishlist | booking | chat_recommend
    Weight FLOAT NOT NULL DEFAULT 1,
    SessionId VARCHAR(64) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_UserTourInteractions_Customer_Tour_Type
    ON UserTourInteractions (CustomerId, TourId, InteractionType);

-- Lịch sử huấn luyện model ML nội bộ
CREATE TABLE ModelTrainingRuns (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ModelName VARCHAR(100) NOT NULL,
    Status VARCHAR(30) NOT NULL, -- Running | Completed | Failed
    TourCount INT NOT NULL DEFAULT 0,
    TourismCount INT NOT NULL DEFAULT 0,
    InteractionCount INT NOT NULL DEFAULT 0,
    IntentAccuracy FLOAT NULL,
    Message NVARCHAR(MAX) NULL,
    StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2 NULL
);

-- Nhãn relevance chuyên gia cho offline evaluation (ground truth)
CREATE TABLE TourRelevanceJudgments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ProfileSignature VARCHAR(32) NOT NULL,
    ProfileQueryKey VARCHAR(128) NULL,
    TourId INT NOT NULL,
    RelevanceGrade INT NOT NULL CHECK (RelevanceGrade BETWEEN 0 AND 3),
    Source VARCHAR(30) NOT NULL DEFAULT 'expert',
    JudgeId VARCHAR(64) NULL,
    Notes NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_TourRelevanceJudgments_Profile_Tour
    ON TourRelevanceJudgments (ProfileSignature, TourId);

-- User study: blind A/B assignments + Likert responses (paper Section 5)
CREATE TABLE UserStudyAssignments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    SessionId VARCHAR(64) NOT NULL,
    ScenarioId INT NOT NULL,
    StrategyForListA VARCHAR(32) NOT NULL,
    StrategyForListB VARCHAR(32) NOT NULL,
    ComparisonPair VARCHAR(64) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_UserStudyAssignments_Session_Scenario UNIQUE (SessionId, ScenarioId)
);

CREATE TABLE UserStudyResponses (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    AssignmentId INT NOT NULL,
    SessionId VARCHAR(64) NOT NULL,
    ScenarioId INT NOT NULL,
    PreferredList VARCHAR(8) NOT NULL,
    FairnessListA INT NOT NULL CHECK (FairnessListA BETWEEN 1 AND 7),
    FairnessListB INT NOT NULL CHECK (FairnessListB BETWEEN 1 AND 7),
    SatisfactionListA INT NOT NULL CHECK (SatisfactionListA BETWEEN 1 AND 7),
    SatisfactionListB INT NOT NULL CHECK (SatisfactionListB BETWEEN 1 AND 7),
    GroupFairnessListA INT NOT NULL CHECK (GroupFairnessListA BETWEEN 1 AND 7),
    GroupFairnessListB INT NOT NULL CHECK (GroupFairnessListB BETWEEN 1 AND 7),
    WouldBookListA INT NOT NULL CHECK (WouldBookListA BETWEEN 1 AND 7),
    WouldBookListB INT NOT NULL CHECK (WouldBookListB BETWEEN 1 AND 7),
    AgeGroup VARCHAR(20) NULL,
    TravelExperience VARCHAR(30) NULL,
    OpenComment NVARCHAR(500) NULL,
    ResponseSource VARCHAR(32) NOT NULL DEFAULT 'human',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT UQ_UserStudyResponses_Session_Scenario UNIQUE (SessionId, ScenarioId),
    CONSTRAINT FK_UserStudyResponses_Assignment FOREIGN KEY (AssignmentId) REFERENCES UserStudyAssignments(Id)
);
GO
USE master;
GO
PRINT '=======================================================';
PRINT 'REBUILD TOÀN BỘ 9 DATABASES STAYHUB THEO HƯỚNG MỚI THÀNH CÔNG!';
PRINT '=======================================================';