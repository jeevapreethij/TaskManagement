SET NOCOUNT ON;
GO

-- Repeatable: inserts only users whose email does not already exist.
INSERT INTO dbo.Users (FullName, Email, DateOfJoining, IsActive, CreatedBy)
SELECT v.FullName, v.Email, v.DateOfJoining, v.IsActive, N'System'
FROM (VALUES
	(N'Asha Verma',    N'asha.verma@example.com',    CAST('2022-01-10' AS DATE), CAST(1 AS BIT)),
	(N'Rahul Sharma',  N'rahul.sharma@example.com',  CAST('2022-06-15' AS DATE), CAST(1 AS BIT)),
	(N'Priya Nair',    N'priya.nair@example.com',    CAST('2023-03-01' AS DATE), CAST(1 AS BIT)),
	(N'Karan Mehta',   N'karan.mehta@example.com',   CAST('2023-09-20' AS DATE), CAST(1 AS BIT)),
	(N'Neha Iyer',     N'neha.iyer@example.com',     CAST('2024-02-12' AS DATE), CAST(1 AS BIT)),
	(N'Old Employee',  N'old.employee@example.com',  CAST('2019-04-05' AS DATE), CAST(0 AS BIT))
) AS v (FullName, Email, DateOfJoining, IsActive)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.Email = v.Email);
GO
