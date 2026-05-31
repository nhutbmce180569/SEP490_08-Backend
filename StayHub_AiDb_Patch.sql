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

IF OBJECT_ID(N'dbo.TourRelevanceJudgments', N'U') IS NULL
BEGIN
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

    PRINT 'Created table TourRelevanceJudgments';
END
ELSE
    PRINT 'Table TourRelevanceJudgments already exists';
GO

IF OBJECT_ID(N'dbo.UserStudyAssignments', N'U') IS NULL
BEGIN
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
    PRINT 'Created table UserStudyAssignments';
END
ELSE
    PRINT 'Table UserStudyAssignments already exists';
GO

IF OBJECT_ID(N'dbo.UserStudyResponses', N'U') IS NULL
BEGIN
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
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT UQ_UserStudyResponses_Session_Scenario UNIQUE (SessionId, ScenarioId),
        CONSTRAINT FK_UserStudyResponses_Assignment FOREIGN KEY (AssignmentId) REFERENCES UserStudyAssignments(Id)
    );
    PRINT 'Created table UserStudyResponses';
END
ELSE
    PRINT 'Table UserStudyResponses already exists';
GO

IF COL_LENGTH('dbo.UserStudyResponses', 'ResponseSource') IS NULL
BEGIN
    ALTER TABLE UserStudyResponses ADD ResponseSource VARCHAR(32) NOT NULL DEFAULT 'human';
    PRINT 'Added UserStudyResponses.ResponseSource column';
END
GO

PRINT 'StayHub_AiDb patch completed.';
GO
