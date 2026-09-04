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

