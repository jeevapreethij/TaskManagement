-- Run against the target database (e.g. USE TaskManagementDb;)
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Users
	(
		UserId        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
		FullName      NVARCHAR(150) NOT NULL,
		Email         NVARCHAR(256) NOT NULL,
		DateOfJoining DATE          NOT NULL,
		IsActive      BIT           NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
		CreatedAt     DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
		CreatedBy     NVARCHAR(150) NOT NULL CONSTRAINT DF_Users_CreatedBy DEFAULT (N'System'),
		UpdatedAt     DATETIME2     NULL,
		UpdatedBy     NVARCHAR(150) NULL,
		CONSTRAINT UQ_Users_Email UNIQUE (Email)
	);
END
GO

IF OBJECT_ID(N'dbo.Tasks', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Tasks
	(
		TaskId         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Tasks PRIMARY KEY,
		TaskReference  UNIQUEIDENTIFIER  NOT NULL CONSTRAINT DF_Tasks_Ref DEFAULT (NEWID()),
		Title          NVARCHAR(200)     NOT NULL,
		Description    NVARCHAR(MAX)     NULL,
		DueDate        DATE              NOT NULL,
		Priority       NVARCHAR(10)      NOT NULL,
		AssignedUserId INT               NOT NULL,
		Status         NVARCHAR(20)      NOT NULL CONSTRAINT DF_Tasks_Status DEFAULT (N'Open'),
		CreatedAt      DATETIME2         NOT NULL CONSTRAINT DF_Tasks_CreatedAt DEFAULT (SYSUTCDATETIME()),
		CreatedBy      NVARCHAR(150)     NOT NULL CONSTRAINT DF_Tasks_CreatedBy DEFAULT (N'System'),
		UpdatedAt      DATETIME2         NULL,
		UpdatedBy      NVARCHAR(150)     NULL,
		ClosedAt       DATETIME2         NULL,
		RowVersion     ROWVERSION        NOT NULL,
		CONSTRAINT UQ_Tasks_TaskReference UNIQUE (TaskReference),
		CONSTRAINT FK_Tasks_Users FOREIGN KEY (AssignedUserId) REFERENCES dbo.Users (UserId),
		CONSTRAINT CK_Tasks_Priority CHECK (Priority IN (N'Low', N'Medium', N'High')),
		CONSTRAINT CK_Tasks_Status   CHECK (Status IN (N'Open', N'In Progress', N'Hold', N'Closed')),
		CONSTRAINT CK_Tasks_Title    CHECK (LEN(LTRIM(RTRIM(Title))) > 0)
	);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tasks_AssignedUserId' AND object_id = OBJECT_ID(N'dbo.Tasks'))
	CREATE INDEX IX_Tasks_AssignedUserId ON dbo.Tasks (AssignedUserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tasks_Status' AND object_id = OBJECT_ID(N'dbo.Tasks'))
	CREATE INDEX IX_Tasks_Status ON dbo.Tasks (Status);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Tasks_DueDate' AND object_id = OBJECT_ID(N'dbo.Tasks'))
	CREATE INDEX IX_Tasks_DueDate ON dbo.Tasks (DueDate);
GO

-- Audit columns (idempotent; also upgrades databases created by earlier versions of this script)
IF COL_LENGTH(N'dbo.Users', N'CreatedBy') IS NULL
    ALTER TABLE dbo.Users ADD CreatedBy NVARCHAR(150) NOT NULL CONSTRAINT DF_Users_CreatedBy DEFAULT (N'System');
GO
IF COL_LENGTH(N'dbo.Users', N'UpdatedBy') IS NULL
    ALTER TABLE dbo.Users ADD UpdatedBy NVARCHAR(150) NULL;
GO
IF COL_LENGTH(N'dbo.Tasks', N'CreatedBy') IS NULL
    ALTER TABLE dbo.Tasks ADD CreatedBy NVARCHAR(150) NOT NULL CONSTRAINT DF_Tasks_CreatedBy DEFAULT (N'System');
GO
IF COL_LENGTH(N'dbo.Tasks', N'UpdatedBy') IS NULL
    ALTER TABLE dbo.Tasks ADD UpdatedBy NVARCHAR(150) NULL;
GO
IF COL_LENGTH(N'dbo.Tasks', N'ClosedAt') IS NULL
    ALTER TABLE dbo.Tasks ADD ClosedAt DATETIME2 NULL;
GO

-- Serilog MSSqlServer sink table (default column layout)
IF OBJECT_ID(N'dbo.Logs', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.Logs
	(
		Id              INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Logs PRIMARY KEY,
		Message         NVARCHAR(MAX) NULL,
		MessageTemplate NVARCHAR(MAX) NULL,
		Level           NVARCHAR(128) NULL,
		TimeStamp       DATETIME2     NOT NULL,
		Exception       NVARCHAR(MAX) NULL,
		Properties      NVARCHAR(MAX) NULL
	);
	CREATE INDEX IX_Logs_TimeStamp ON dbo.Logs (TimeStamp DESC);
END
GO
