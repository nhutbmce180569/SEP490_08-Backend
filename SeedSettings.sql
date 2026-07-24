USE StayHub_SystemDb;
GO
SET IDENTITY_INSERT SystemSettings ON;

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'WebLogo')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'WebLogo', N'', N'Web Admin & Portal Logo URL', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'AppLogo')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'AppLogo', N'', N'Mobile App Splash Logo URL', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'CompanyName')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'CompanyName', N'StayHub', N'Company Name', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'CompanyPhone')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'CompanyPhone', N'1900 6868', N'Company Support Phone', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'CompanyEmail')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'CompanyEmail', N'support@stayhub.vn', N'Company Support Email', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'CompanyAddress')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'CompanyAddress', N'FPT University', N'Company Headquarter Address', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'MapIframeUrl')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'MapIframeUrl', N'', N'Google Map Embed URL', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'AboutUs')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'AboutUs', N'Welcome to StayHub.', N'About Us Content (Markdown)', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'StayHubReviews')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'StayHubReviews', N'', N'StayHub Platform Reviews Info', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'ContactUs')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'ContactUs', N'', N'Contact Us Information', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'TravelGuides')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'TravelGuides', N'', N'Travel Guides Content', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'PrivacyPolicy')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'PrivacyPolicy', N'', N'Privacy Policy', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'TermsAndConditions')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'TermsAndConditions', N'', N'Terms & Conditions', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'DataPolicy')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'DataPolicy', N'', N'Data Policy', GETDATE());

IF NOT EXISTS (SELECT 1 FROM SystemSettings WHERE SettingKey = 'CookiePolicy')
    INSERT INTO SystemSettings (Id, SettingKey, SettingValue, Description, UpdatedAt) VALUES ((SELECT ISNULL(MAX(Id), 0) + 1 FROM SystemSettings), 'CookiePolicy', N'', N'Cookie Policy', GETDATE());

SET IDENTITY_INSERT SystemSettings OFF;
