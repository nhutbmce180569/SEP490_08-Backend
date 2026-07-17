/* =========================================================
   STAYHUB SYSTEM DB DATA SEED
   ========================================================= */
USE StayHub_SystemDb;
GO

-- 1. SystemSettings
SET IDENTITY_INSERT SystemSettings ON;
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 1)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (1, 'BOOKING_HOLD_MINUTES', N'15', N'Seat holding time when creating an unpaid order.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 2)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (2, 'CANCELLATION_FEE_PERCENT', N'10', N'Default tour cancellation fee percentage.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 3)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (3, 'MAX_TICKET_PER_ORDER', N'10', N'Maximum number of tickets in an order.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 4)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (4, 'SUPPORT_EMAIL', N'support@stayhub.vn', N'Customer support email.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 5)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (5, 'SUPPORT_PHONE', N'1900 6868', N'Support hotline number.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 6)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (6, 'LOCATION_LOG_INTERVAL_SECONDS', N'60', N'Location sending interval during the tour.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 7)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (7, 'MOMENT_MAX_IMAGE_MB', N'5', N'Maximum image size for moments.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 8)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (8, 'REVIEW_EDIT_DAYS', N'7', N'Number of days allowed to edit a review.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 9)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (9, 'DEFAULT_COUNTRY', N'Vietnam', N'System default country.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 10)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (10, 'PAYMENT_PROVIDER_DEFAULT', N'VNPay', N'Default payment gateway.', GETDATE());
IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE Id = 11)
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES (11, 'INAPPROPRIATE_WORDS_FILTER', N'fuck,dm,vl,cl,dcm,buoi,cac,spam,scam,chui,dit,du,lon,fucking,scammer', N'Comma separated list of banned/inappropriate words.', GETDATE());
SET IDENTITY_INSERT SystemSettings OFF;
GO

-- 2. Notifications
SET IDENTITY_INSERT Notifications ON;
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 1)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (1, 8, N'Booking successful', N'Your Phu Quoc order has been confirmed.', 0, DATEADD(DAY, -24, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 2)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (2, 9, N'Payment successful', N'The transaction for the Ha Long cruise has been completed.', 1, DATEADD(DAY, -22, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 3)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (3, 10, N'Check-in successful', N'Your Hoi An ticket has been checked in.', 1, DATEADD(DAY, -20, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 4)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (4, 11, N'Tour reminder', N'Your Mekong Delta tour is departing soon.', 0, DATEADD(DAY, -18, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 5)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (5, 12, N'Tour cancellation request', N'Your tour cancellation request is pending.', 0, DATEADD(DAY, -5, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 6)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (6, 13, N'Honeymoon setup', N'StayHub has noted your honeymoon setup request.', 1, DATEADD(DAY, -14, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 7)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (7, 14, N'Thank you for your review', N'Your review helps the community choose better tours.', 1, DATEADD(DAY, -12, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 8)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (8, 15, N'Refund successful', N'The partial refund request has been processed.', 0, DATEADD(DAY, -1, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 9)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (9, 16, N'Order cancelled', N'Your Da Lat order has been cancelled.', 1, DATEADD(DAY, -8, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 10)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (10, 17, N'New voucher', N'You just received the DANANG300 voucher.', 0, DATEADD(DAY, -7, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 11)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (11, 4, N'Schedule assignment', N'You are assigned to the Phu Quoc tour on 01/06/2026.', 0, DATEADD(DAY, -3, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 12)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (12, 5, N'Schedule assignment', N'You are assigned to assist the Ha Long tour.', 0, DATEADD(DAY, -2, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 13)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (13, 183, N'New Friend Request', N'customer.an@gmail.com sent you a friend request.', 0, DATEADD(DAY, -1, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 14)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (14, 183, N'Group Chat Joined', N'You have been added to the Nha Trang Tour Group chat room.', 1, DATEADD(DAY, -2, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 15)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (15, 403, N'New Moment Comment', N'Vo Huong Giang commented on your Hoi An moment.', 0, DATEADD(DAY, -1, GETDATE()));
IF NOT EXISTS (SELECT 1 FROM Notifications WHERE Id = 16)
    INSERT INTO Notifications (Id, UserId, Title, Content, IsRead, CreatedAt) VALUES (16, 59, N'Moment Flagged', N'Your recent moment has been flagged by system auto-moderation.', 0, DATEADD(DAY, -2, GETDATE()));
SET IDENTITY_INSERT Notifications OFF;
GO

PRINT 'SUCCESSFULLY POPULATED STAYHUB SYSTEM DB DATA SEED!';
GO
