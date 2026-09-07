/*
  Rename CustomerServiceRequests pre-assignment status: Pending -> Initiated.
  Also updates the live CHECK constraint and column default.
  Safe / idempotent for SQL Server (SahulatAppDB).
*/

-- 1) Drop existing Status CHECK (blocks Initiated today)
DECLARE @ck sysname;
SELECT @ck = cc.name
FROM sys.check_constraints cc
WHERE cc.parent_object_id = OBJECT_ID(N'dbo.CustomerServiceRequests')
  AND cc.definition LIKE N'%Status%';

IF @ck IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.CustomerServiceRequests DROP CONSTRAINT [' + @ck + N']');
END
GO

-- 2) Rename existing rows
UPDATE dbo.CustomerServiceRequests
SET Status = N'Initiated'
WHERE Status = N'Pending';
GO

-- 3) Recreate CHECK with Initiated (replaces Pending)
IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.CustomerServiceRequests')
      AND name = N'CK_CustomerServiceRequests_Status'
)
BEGIN
    ALTER TABLE dbo.CustomerServiceRequests
    ADD CONSTRAINT CK_CustomerServiceRequests_Status CHECK (
        [Status] IN (
            N'Initiated',
            N'Assigned',
            N'Accepted',
            N'In Progress',
            N'Completed',
            N'Cancelled',
            N'Rejected'
        )
    );
END
GO

-- 4) Replace Status DEFAULT with Initiated
DECLARE @df sysname;
SELECT @df = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON c.default_object_id = dc.object_id
   AND c.object_id = dc.parent_object_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.CustomerServiceRequests')
  AND c.name = N'Status';

IF @df IS NOT NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.CustomerServiceRequests DROP CONSTRAINT [' + @df + N']');
END

ALTER TABLE dbo.CustomerServiceRequests
ADD CONSTRAINT DF_CustomerServiceRequests_Status DEFAULT (N'Initiated') FOR Status;
GO
