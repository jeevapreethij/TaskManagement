SET NOCOUNT ON;
GO

/* Result codes for write procedures (returned as: SELECT ResultCode):
	 0 = Success, 1 = Task not found, 2 = Concurrency conflict,
	 3 = Assigned user does not exist or is inactive, 4 = Invalid input */

CREATE OR ALTER PROCEDURE dbo.usp_User_GetActiveForAssignment
AS
BEGIN
	SET NOCOUNT ON;
	SELECT UserId, FullName, Email
	FROM dbo.Users
	WHERE IsActive = 1
	ORDER BY FullName;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Task_Create
	@Title          NVARCHAR(200),
	@Description    NVARCHAR(MAX) = NULL,
	@DueDate        DATE,
	@Priority       NVARCHAR(10),
	@AssignedUserId INT,
	@CreatedBy      NVARCHAR(150) = N'System'
AS
BEGIN
	SET NOCOUNT ON;

	IF @Title IS NULL OR LEN(LTRIM(RTRIM(@Title))) = 0
	   OR @Priority NOT IN (N'Low', N'Medium', N'High')
	BEGIN
		SELECT 4 AS ResultCode, CAST(NULL AS INT)
		RETURN;
	END

	IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @AssignedUserId AND IsActive = 1)
	BEGIN
		SELECT 3 AS ResultCode, CAST(NULL AS INT) AS TaskId, CAST(NULL AS UNIQUEIDENTIFIER) AS TaskReference;
		RETURN;
	END

	DECLARE @Ref UNIQUEIDENTIFIER = NEWID();

	INSERT INTO dbo.Tasks (TaskReference, Title, Description, DueDate, Priority, AssignedUserId, Status, CreatedBy)
	VALUES (@Ref, LTRIM(RTRIM(@Title)), @Description, @DueDate, @Priority, @AssignedUserId, N'Open', ISNULL(NULLIF(LTRIM(RTRIM(@CreatedBy)), N''), N'System'));

	SELECT 0 AS ResultCode, CAST(SCOPE_IDENTITY() AS INT) AS TaskId, @Ref AS TaskReference;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Task_GetByReference
	@TaskReference UNIQUEIDENTIFIER
AS
BEGIN
	SET NOCOUNT ON;
	SELECT t.TaskId, t.TaskReference, t.Title, t.Description, t.DueDate, t.Priority,
		   t.AssignedUserId, u.FullName AS AssignedUserName, u.IsActive AS AssignedUserIsActive,
		   t.Status, t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy, t.ClosedAt, t.RowVersion
	FROM dbo.Tasks t
	INNER JOIN dbo.Users u ON u.UserId = t.AssignedUserId
	WHERE t.TaskReference = @TaskReference;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Task_GetById
	@TaskId INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT t.TaskId, t.TaskReference, t.Title, t.Description, t.DueDate, t.Priority,
		   t.AssignedUserId, u.FullName AS AssignedUserName, u.IsActive AS AssignedUserIsActive,
		   t.Status, t.CreatedAt, t.CreatedBy, t.UpdatedAt, t.UpdatedBy, t.ClosedAt, t.RowVersion
	FROM dbo.Tasks t
	INNER JOIN dbo.Users u ON u.UserId = t.AssignedUserId
	WHERE t.TaskId = @TaskId;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Task_Update
	@TaskReference  UNIQUEIDENTIFIER,
	@Title          NVARCHAR(200),
	@Description    NVARCHAR(MAX) = NULL,
	@DueDate        DATE,
	@Priority       NVARCHAR(10),
	@AssignedUserId INT,
	@RowVersion     BINARY(8) = NULL,
	@UpdatedBy      NVARCHAR(150) = N'System'
AS
BEGIN
	SET NOCOUNT ON;

	IF @Title IS NULL
	   OR @Priority NOT IN (N'Low', N'Medium', N'High')
	BEGIN SELECT 4 AS ResultCode; RETURN; END

	DECLARE @CurrentUserId INT;
	SELECT @CurrentUserId = AssignedUserId FROM dbo.Tasks WHERE TaskReference = @TaskReference;

	IF @CurrentUserId IS NULL BEGIN SELECT 1 AS ResultCode; RETURN; END

	IF @AssignedUserId <> @CurrentUserId
	   AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @AssignedUserId AND IsActive = 1)
	BEGIN SELECT 3 AS ResultCode; RETURN; END

	UPDATE dbo.Tasks
	SET Title = LTRIM(RTRIM(@Title)), Description = @Description, DueDate = @DueDate,
		Priority = @Priority, AssignedUserId = @AssignedUserId, UpdatedAt = SYSUTCDATETIME(),
		UpdatedBy = ISNULL(NULLIF(LTRIM(RTRIM(@UpdatedBy)), N''), N'System')
	WHERE TaskReference = @TaskReference
	  AND (@RowVersion IS NULL OR RowVersion = @RowVersion);

	IF @@ROWCOUNT = 0 SELECT 2 AS ResultCode; ELSE SELECT 0 AS ResultCode;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Task_UpdateStatus
	@TaskReference UNIQUEIDENTIFIER,
	@Status        NVARCHAR(20),
	@RowVersion    BINARY(8) = NULL,
	@UpdatedBy     NVARCHAR(150) = N'System'
AS
BEGIN
	SET NOCOUNT ON;

	IF @Status NOT IN (N'Open', N'In Progress', N'Hold', N'Closed')
	BEGIN SELECT 4 AS ResultCode; RETURN; END

	IF NOT EXISTS (SELECT 1 FROM dbo.Tasks WHERE TaskReference = @TaskReference)
	BEGIN SELECT 1 AS ResultCode; RETURN; END

	UPDATE dbo.Tasks
	SET Status = @Status, UpdatedAt = SYSUTCDATETIME(),
		UpdatedBy = ISNULL(NULLIF(LTRIM(RTRIM(@UpdatedBy)), N''), N'System'),
		ClosedAt = CASE WHEN @Status = N'Closed' THEN ISNULL(ClosedAt, SYSUTCDATETIME()) ELSE NULL END
	WHERE TaskReference = @TaskReference
	  AND (@RowVersion IS NULL OR RowVersion = @RowVersion);

	IF @@ROWCOUNT = 0 SELECT 2 AS ResultCode; ELSE SELECT 0 AS ResultCode;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Task_AssignUser
	@TaskReference  UNIQUEIDENTIFIER,
	@AssignedUserId INT,
	@RowVersion     BINARY(8) = NULL,
	@UpdatedBy      NVARCHAR(150) = N'System'
AS
BEGIN
	SET NOCOUNT ON;

	IF NOT EXISTS (SELECT 1 FROM dbo.Tasks WHERE TaskReference = @TaskReference)
	BEGIN SELECT 1 AS ResultCode; RETURN; END

	IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = @AssignedUserId AND IsActive = 1)
	BEGIN SELECT 3 AS ResultCode; RETURN; END

	UPDATE dbo.Tasks
	SET AssignedUserId = @AssignedUserId, UpdatedAt = SYSUTCDATETIME(),
		UpdatedBy = ISNULL(NULLIF(LTRIM(RTRIM(@UpdatedBy)), N''), N'System')
	WHERE TaskReference = @TaskReference
	  AND (@RowVersion IS NULL OR RowVersion = @RowVersion);

	IF @@ROWCOUNT = 0 SELECT 2 AS ResultCode; ELSE SELECT 0 AS ResultCode;
END
GO

/* Search contract: blank term = no text filter; numeric term matches TaskId;
   GUID term matches TaskReference; otherwise Title LIKE %term% (wildcards escaped). */
CREATE OR ALTER PROCEDURE dbo.usp_Task_Search
	@SearchTerm     NVARCHAR(100) = NULL,
	@AssignedUserId INT           = NULL,
	@Status         NVARCHAR(20)  = NULL,
	@Priority       NVARCHAR(10)  = NULL,
	@PageNumber     INT           = 1,
	@PageSize       INT           = 20
AS
BEGIN
	SET NOCOUNT ON;

	IF @PageNumber < 1 SET @PageNumber = 1;
	IF @PageSize   < 1 SET @PageSize   = 20;
	IF @PageSize   > 100 SET @PageSize = 100;

	DECLARE @Term NVARCHAR(100) = NULLIF(LTRIM(RTRIM(@SearchTerm)), N'');
	DECLARE @TermId INT = TRY_CONVERT(INT, @Term);
	DECLARE @TermRef UNIQUEIDENTIFIER = TRY_CONVERT(UNIQUEIDENTIFIER, @Term);
	DECLARE @Like NVARCHAR(250) = NULL;

	IF @Term IS NOT NULL
		SET @Like = N'%' + REPLACE(REPLACE(REPLACE(REPLACE(@Term, N'\', N'\\'), N'%', N'\%'), N'_', N'\_'), N'[', N'\[') + N'%';

	SELECT t.TaskId, t.TaskReference, t.Title, t.DueDate, t.Priority, t.Status,
		   t.AssignedUserId, u.FullName AS AssignedUserName, t.CreatedAt, t.UpdatedAt,
		   COUNT(*) OVER () AS TotalCount
	FROM dbo.Tasks t
	INNER JOIN dbo.Users u ON u.UserId = t.AssignedUserId
	WHERE (@AssignedUserId IS NULL OR t.AssignedUserId = @AssignedUserId)
	  AND (@Status   IS NULL OR t.Status   = @Status)
	  AND (@Priority IS NULL OR t.Priority = @Priority)
	  AND (@Term IS NULL
		   OR t.TaskId = @TermId
		   OR t.TaskReference = @TermRef
		   OR t.Title LIKE @Like ESCAPE N'\')
	ORDER BY t.DueDate ASC, t.TaskId ASC
	OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO

CREATE OR ALTER PROCEDURE dbo.usp_Task_GetAll
	@PageNumber INT = 1,
	@PageSize   INT = 20
AS
BEGIN
	SET NOCOUNT ON;

	IF @PageNumber < 1 SET @PageNumber = 1;
	IF @PageSize   < 1 SET @PageSize   = 20;
	IF @PageSize   > 100 SET @PageSize = 100;

	SELECT t.TaskId, t.TaskReference, t.Title, t.DueDate, t.Priority, t.Status,
		   t.AssignedUserId, u.FullName AS AssignedUserName, t.CreatedAt, t.UpdatedAt,
		   COUNT(*) OVER () AS TotalCount
	FROM dbo.Tasks t
	INNER JOIN dbo.Users u ON u.UserId = t.AssignedUserId
	ORDER BY t.DueDate ASC, t.TaskId ASC
	OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
