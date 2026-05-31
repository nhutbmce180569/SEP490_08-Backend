-- Patch cho DB StayHub_AiDb đã tạo trước đó (không cần drop/rebuild toàn bộ)
-- Chạy file này nếu gặp lỗi: Invalid object name 'UserTourInteractions'

USE StayHub_AiDb;
GO

IF OBJECT_ID(N'dbo.UserTourInteractions', N'U') IS NULL
BEGIN
    CREATE TABLE UserTourInteractions (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CustomerId INT NULL,
        TourId INT NOT NULL,
        InteractionType VARCHAR(50) NOT NULL,
        Weight FLOAT NOT NULL DEFAULT 1,
        SessionId VARCHAR(64) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IX_UserTourInteractions_Customer_Tour_Type
        ON UserTourInteractions (CustomerId, TourId, InteractionType);

    PRINT 'Created table UserTourInteractions';
END
ELSE
    PRINT 'Table UserTourInteractions already exists';
GO

IF OBJECT_ID(N'dbo.ModelTrainingRuns', N'U') IS NULL
BEGIN
    CREATE TABLE ModelTrainingRuns (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ModelName VARCHAR(100) NOT NULL,
        Status VARCHAR(30) NOT NULL,
        TourCount INT NOT NULL DEFAULT 0,
        TourismCount INT NOT NULL DEFAULT 0,
        InteractionCount INT NOT NULL DEFAULT 0,
        IntentAccuracy FLOAT NULL,
        Message NVARCHAR(MAX) NULL,
        StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CompletedAt DATETIME2 NULL
    );

    PRINT 'Created table ModelTrainingRuns';
END
ELSE
    PRINT 'Table ModelTrainingRuns already exists';
GO

PRINT 'StayHub_AiDb patch completed.';
GO
