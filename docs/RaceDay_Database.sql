/* =========================================================
   RaceDay Event Management System
   PROG6212 PoE Part 1 - Database Creation Script
   Student: ST10465060
   ========================================================= */

IF DB_ID('RaceDayDB') IS NOT NULL
BEGIN
    ALTER DATABASE RaceDayDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE RaceDayDB;
END
GO

CREATE DATABASE RaceDayDB;
GO

USE RaceDayDB;
GO

/* ---------- 1. Roles ---------- */
CREATE TABLE Roles (
    RoleId   INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(20) NOT NULL UNIQUE
);
GO

/* ---------- 2. Users ---------- */
CREATE TABLE Users (
    UserId       INT IDENTITY(1,1) PRIMARY KEY,
    FirstName    NVARCHAR(50)  NOT NULL,
    LastName     NVARCHAR(50)  NOT NULL,
    Email        NVARCHAR(120) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    PhoneNumber  NVARCHAR(20)  NULL,
    DateOfBirth  DATE          NULL,
    RoleId       INT           NOT NULL,
    CreatedAt    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);
GO

/* ---------- 3. Venues ---------- */
CREATE TABLE Venues (
    VenueId   INT IDENTITY(1,1) PRIMARY KEY,
    VenueName NVARCHAR(100) NOT NULL,
    City      NVARCHAR(60)  NOT NULL,
    Province  NVARCHAR(60)  NOT NULL,
    Capacity  INT           NULL,
    CONSTRAINT UQ_Venue_Name_City UNIQUE (VenueName, City),
    CONSTRAINT CK_Venue_Capacity CHECK (Capacity IS NULL OR Capacity > 0)
);
GO

/* ---------- 4. Events ---------- */
CREATE TABLE Events (
    EventId     INT IDENTITY(1,1) PRIMARY KEY,
    EventName   NVARCHAR(120) NOT NULL,
    Description NVARCHAR(500) NULL,
    EventDate   DATE          NOT NULL,
    VenueId     INT           NOT NULL,
    OrganiserId INT           NOT NULL,
    Status      NVARCHAR(20)  NOT NULL DEFAULT 'Open',
    CreatedAt   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Events_Venues     FOREIGN KEY (VenueId)     REFERENCES Venues(VenueId),
    CONSTRAINT FK_Events_Organiser  FOREIGN KEY (OrganiserId) REFERENCES Users(UserId),
    CONSTRAINT CK_Events_Status CHECK (Status IN ('Draft','Open','Closed','Completed','Cancelled'))
);
GO

/* ---------- 5. Categories ---------- */
CREATE TABLE Categories (
    CategoryId      INT IDENTITY(1,1) PRIMARY KEY,
    EventId         INT           NOT NULL,
    CategoryName    NVARCHAR(60)  NOT NULL,
    DistanceKm      DECIMAL(5,2)  NOT NULL,
    EntryFee        DECIMAL(10,2) NOT NULL DEFAULT 0.00,
    MaxParticipants INT           NULL,
    CONSTRAINT FK_Categories_Events FOREIGN KEY (EventId) REFERENCES Events(EventId) ON DELETE CASCADE,
    CONSTRAINT UQ_Category_PerEvent UNIQUE (EventId, CategoryName),
    CONSTRAINT CK_Category_Distance CHECK (DistanceKm > 0)
);
GO

/* ---------- 6. Enrolments (resolves Users <-> Categories M:N) ---------- */
CREATE TABLE Enrolments (
    EnrolmentId   INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId    INT          NOT NULL,
    ParticipantId INT          NOT NULL,
    EnrolmentDate DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME(),
    RaceNumber    NVARCHAR(10) NULL,
    Status        NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    CONSTRAINT FK_Enrolments_Categories   FOREIGN KEY (CategoryId)    REFERENCES Categories(CategoryId),
    CONSTRAINT FK_Enrolments_Participants FOREIGN KEY (ParticipantId) REFERENCES Users(UserId),
    CONSTRAINT UQ_Enrolment_Once UNIQUE (CategoryId, ParticipantId),
    CONSTRAINT CK_Enrolment_Status CHECK (Status IN ('Pending','Confirmed','Cancelled'))
);
GO

/* ---------- 7. Results ---------- */
CREATE TABLE Results (
    ResultId    INT IDENTITY(1,1) PRIMARY KEY,
    EnrolmentId INT       NOT NULL UNIQUE,
    FinishTime  TIME(0)   NULL,
    Position    INT       NULL,
    RecordedAt  DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Results_Enrolments FOREIGN KEY (EnrolmentId) REFERENCES Enrolments(EnrolmentId),
    CONSTRAINT CK_Result_Position CHECK (Position IS NULL OR Position > 0)
);
GO

USE RaceDayDB;
GO

