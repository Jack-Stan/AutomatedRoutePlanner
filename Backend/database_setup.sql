-- Complete Database Setup Script voor Hoppy Route Planning
-- Dit script maakt eerst alle tabellen aan en voegt dan test data toe

USE [Hoppydb]
GO

-- 1. Zones tabel aanmaken
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Zones' AND xtype='U')
CREATE TABLE [dbo].[Zones] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Boundary] geography NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Zones] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

-- 2. Users tabel aanmaken
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
CREATE TABLE [dbo].[Users] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Email] nvarchar(255) NOT NULL,
    [PasswordHash] nvarchar(255) NOT NULL,
    [FirstName] nvarchar(100) NULL,
    [LastName] nvarchar(100) NULL,
    [Role] int NOT NULL DEFAULT 0, -- 0 = User, 1 = Admin, 2 = Driver
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [PasswordResetToken] nvarchar(255) NULL,
    [PasswordResetExpires] datetime2(7) NULL,
    [TemporaryPassword] nvarchar(255) NULL,
    [TemporaryPasswordExpires] datetime2(7) NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
)
GO

-- 3. ParkingZones tabel aanmaken
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ParkingZones' AND xtype='U')
CREATE TABLE [dbo].[ParkingZones] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [ZoneId] int NOT NULL,
    [Latitude] decimal(18,6) NOT NULL,
    [Longitude] decimal(18,6) NOT NULL,
    [Capacity] int NOT NULL DEFAULT 10,
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_ParkingZones] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ParkingZones_Zones] FOREIGN KEY ([ZoneId]) REFERENCES [Zones]([Id])
)
GO

-- 4. Vehicles tabel aanmaken
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Vehicles' AND xtype='U')
CREATE TABLE [dbo].[Vehicles] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [ExternalId] nvarchar(50) NOT NULL,
    [RegistrationNumber] nvarchar(20) NOT NULL,
    [VehicleType] nvarchar(20) NOT NULL, -- 'E-Scooter', 'E-Bike'
    [ZoneId] int NOT NULL,
    [CurrentParkingZoneId] int NULL,
    [Latitude] decimal(18,6) NOT NULL,
    [Longitude] decimal(18,6) NOT NULL,
    [BatteryLevel] int NOT NULL DEFAULT 100, -- 0-100 percentage
    [NeedsBatteryReplacement] bit NOT NULL DEFAULT 0,
    [IsAvailable] bit NOT NULL DEFAULT 1,
    [LastUpdated] datetime2(7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Vehicles] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Vehicles_Zones] FOREIGN KEY ([ZoneId]) REFERENCES [Zones]([Id]),
    CONSTRAINT [FK_Vehicles_ParkingZones] FOREIGN KEY ([CurrentParkingZoneId]) REFERENCES [ParkingZones]([Id]),
    CONSTRAINT [UQ_Vehicles_ExternalId] UNIQUE ([ExternalId]),
    CONSTRAINT [UQ_Vehicles_RegistrationNumber] UNIQUE ([RegistrationNumber])
)
GO

-- 5. Routes tabel aanmaken
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Routes' AND xtype='U')
CREATE TABLE [dbo].[Routes] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [ZoneId] int NOT NULL,
    [AssignedDriverId] int NULL,
    [Status] int NOT NULL DEFAULT 0, -- 0 = Planned, 1 = InProgress, 2 = Completed, 3 = Cancelled
    [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
    [StartedAt] datetime2(7) NULL,
    [CompletedAt] datetime2(7) NULL,
    CONSTRAINT [PK_Routes] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Routes_Zones] FOREIGN KEY ([ZoneId]) REFERENCES [Zones]([Id]),
    CONSTRAINT [FK_Routes_Users] FOREIGN KEY ([AssignedDriverId]) REFERENCES [Users]([Id])
)
GO

-- 6. RouteStops tabel aanmaken
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='RouteStops' AND xtype='U')
CREATE TABLE [dbo].[RouteStops] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [RouteId] int NOT NULL,
    [VehicleId] int NOT NULL,
    [StopOrder] int NOT NULL,
    [Latitude] decimal(18,6) NOT NULL,
    [Longitude] decimal(18,6) NOT NULL,
    [Status] int NOT NULL DEFAULT 0, -- 0 = Planned, 1 = Completed, 2 = Skipped
    [CompletedAt] datetime2(7) NULL,
    CONSTRAINT [PK_RouteStops] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_RouteStops_Routes] FOREIGN KEY ([RouteId]) REFERENCES [Routes]([Id]),
    CONSTRAINT [FK_RouteStops_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([Id])
)
GO

-- 7. Swappers tabel aanmaken
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Swappers' AND xtype='U')
CREATE TABLE [dbo].[Swappers] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserId] int NOT NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Swappers] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_Swappers_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id])
)
GO

-- 8. Regions tabel aanmaken
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Regions' AND xtype='U')
CREATE TABLE [dbo].[Regions] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(100) NOT NULL,
    [Boundary] geography NULL,
    [IsActive] bit NOT NULL DEFAULT 1,
    [CreatedAt] datetime2(7) NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [PK_Regions] PRIMARY KEY CLUSTERED ([Id] ASC)
)
GO

PRINT 'Database schema aangemaakt!'

-- Nu basis data invoegen
PRINT 'Basis data invoegen...'

-- Zone Kortrijk toevoegen (ID = 2 zoals in je originele script)
IF NOT EXISTS (SELECT * FROM Zones WHERE Id = 2)
BEGIN
    SET IDENTITY_INSERT Zones ON
    INSERT INTO Zones (Id, Name, IsActive, CreatedAt, UpdatedAt)
    VALUES (2, 'Kortrijk', 1, GETDATE(), GETDATE())
    SET IDENTITY_INSERT Zones OFF
    PRINT 'Zone Kortrijk toegevoegd'
END

-- Test admin user toevoegen
IF NOT EXISTS (SELECT * FROM Users WHERE Email = 'admin@hoppy.be')
BEGIN
    INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Role, IsActive)
    VALUES ('admin@hoppy.be', 'AQAAAAIAAYagAAAAEGpW8x2nZqUqVz5KqJJ+123abc', 'Admin', 'User', 1, 1)
    PRINT 'Admin user toegevoegd'
END

PRINT 'Basis data toegevoegd!'
PRINT 'Database setup voltooid. Nu kun je de vehicles data invoegen.'
