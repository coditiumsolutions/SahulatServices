-- Seed 10 ServiceRequests per active ServiceCategory
SET NOCOUNT ON;

WITH Numbers AS (
    SELECT 1 AS n UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5
    UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10
),
Categories AS (
    SELECT UID, CategoryName
    FROM ServiceCategories
    WHERE IsActive = 1 OR IsActive IS NULL
)
INSERT INTO ServiceRequests (
    CustomerUID,
    CategoryUID,
    ServiceAddress,
    Latitude,
    Longitude,
    ProblemDescription,
    RequestDate,
    Status
)
SELECT
    ((n.n - 1) % 3) + 1,
    c.UID,
    'House ' + CAST(n.n AS varchar(10)) + ', Sector ' + CAST(c.UID AS varchar(10)) + ', Lahore, Pakistan',
    CAST(31.5200000 + (c.UID * 0.0010000) + (n.n * 0.0001000) AS decimal(10, 7)),
    CAST(74.3587000 + (c.UID * 0.0010000) + (n.n * 0.0001000) AS decimal(10, 7)),
    'Service request #' + CAST(n.n AS varchar(10)) + ' for ' + c.CategoryName + ' - customer needs assistance.',
    DATEADD(DAY, -n.n, GETDATE()),
    CASE (n.n % 4)
        WHEN 0 THEN 'Pending'
        WHEN 1 THEN 'Accepted'
        WHEN 2 THEN 'In Progress'
        ELSE 'Completed'
    END
FROM Categories c
CROSS JOIN Numbers n;

SELECT @@ROWCOUNT AS InsertedRows;

SELECT CategoryUID, COUNT(*) AS RequestCount
FROM ServiceRequests
GROUP BY CategoryUID
ORDER BY CategoryUID;
