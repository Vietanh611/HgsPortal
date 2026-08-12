-- Create DisplayDevices schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'DisplayDevices')
BEGIN
    EXEC('CREATE SCHEMA DisplayDevices');
END
GO

-- Create DisplayDevices table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'DisplayDevices' AND schema_id = SCHEMA_ID('DisplayDevices'))
BEGIN
    CREATE TABLE DisplayDevices.DisplayDevices (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        DeviceName NVARCHAR(100) NOT NULL,
        DeviceIdentifier NVARCHAR(200) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
        LastSeenAt DATETIME2 NULL,
        IsEnabled BIT NOT NULL DEFAULT 1
    );
END
GO

-- Add IsEnabled column if table exists but column doesn't
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DisplayDevices' AND schema_id = SCHEMA_ID('DisplayDevices'))
AND NOT EXISTS (SELECT * FROM sys.columns WHERE name = 'IsEnabled' AND object_id = OBJECT_ID('DisplayDevices.DisplayDevices'))
BEGIN
    ALTER TABLE DisplayDevices.DisplayDevices
    ADD IsEnabled BIT NOT NULL DEFAULT 1;
END
GO