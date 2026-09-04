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

/* ================= SEED DATA ================= */
INSERT INTO Roles (RoleName) VALUES ('Organiser'), ('Participant');

INSERT INTO Users (FirstName, LastName, Email, PasswordHash, PhoneNumber, DateOfBirth, RoleId)
VALUES
 ('Nomsa','Dlamini','nomsa.dlamini@raceday.co.za','SEEDED_HASH_PLACEHOLDER_01','0821234567','1988-04-12',1),
 ('Pieter','van Wyk','pieter.vanwyk@raceday.co.za','SEEDED_HASH_PLACEHOLDER_02','0837654321','1985-09-30',1),
 ('Thandi','Mokoena','thandi.mokoena@gmail.com',   'SEEDED_HASH_PLACEHOLDER_03','0731112223','1997-01-22',2),
 ('Ryan','Naidoo','ryan.naidoo@gmail.com',         'SEEDED_HASH_PLACEHOLDER_04','0794445556','1994-11-05',2);

INSERT INTO Venues (VenueName, City, Province, Capacity)
VALUES
 ('Green Point Athletics Track','Cape Town','Western Cape', 5000),
 ('Zoo Lake Park','Johannesburg','Gauteng', 3500),
 ('Golden Mile Promenade','Durban','KwaZulu-Natal', 8000);

INSERT INTO Events (EventName, Description, EventDate, VenueId, OrganiserId, Status)
VALUES
 ('Cape Town Summer Series','Coastal road race with three distances.','2026-11-14',1,1,'Open'),
 ('Joburg City Night Run','Evening run through the northern suburbs.','2026-10-03',2,1,'Open'),
 ('Durban Beachfront Challenge','Flat, fast promenade course.','2026-12-05',3,2,'Open');

INSERT INTO Categories (EventId, CategoryName, DistanceKm, EntryFee, MaxParticipants)
VALUES
 (1,'5km Fun Run',  5.00, 120.00, 800),
 (1,'10km Road',   10.00, 180.00, 600),
 (1,'21.1km Half', 21.10, 320.00, 400),
 (2,'5km Night Dash', 5.00, 100.00, 500),
 (2,'10km Night Run',10.00, 160.00, 500),
 (3,'10km Promenade',10.00, 150.00, 700),
 (3,'21.1km Half',   21.10, 300.00, 350);

INSERT INTO Enrolments (CategoryId, ParticipantId, RaceNumber, Status)
VALUES
 (2,3,'A1042','Confirmed'),
 (3,4,'A2118','Confirmed'),
 (4,3,'N0071','Confirmed'),
 (6,4,'D3390','Pending');

INSERT INTO Results (EnrolmentId, FinishTime, Position)
VALUES
 (1,'00:52:14', 37),
 (2,'01:58:46',112);
GO

/* ---- Verification queries for the video demo ---- */
SELECT u.FirstName + ' ' + u.LastName AS Participant, e.EventName, c.CategoryName, en.Status
FROM Enrolments en
JOIN Users u      ON u.UserId = en.ParticipantId
JOIN Categories c ON c.CategoryId = en.CategoryId
JOIN Events e     ON e.EventId = c.EventId
ORDER BY e.EventDate;
GO