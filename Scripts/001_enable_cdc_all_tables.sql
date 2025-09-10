-- First, enable CDC at the database level (if not already enabled)
-- This must be done by a sysadmin
EXEC sys.sp_cdc_enable_db;

-- Script to enable CDC on all user tables in the current database
DECLARE @sql NVARCHAR(MAX) = '';
DECLARE @table_name NVARCHAR(128);
DECLARE @schema_name NVARCHAR(128);
DECLARE @full_table_name NVARCHAR(256);

-- Cursor to iterate through all user tables
DECLARE table_cursor CURSOR FOR
    SELECT
        t.name AS table_name,
        s.name AS schema_name,
        QUOTENAME(s.name) + '.' + QUOTENAME(t.name) AS full_table_name
    FROM sys.tables t
             INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE t.is_ms_shipped = 0  -- Exclude system tables
      AND NOT EXISTS (
        -- Skip tables that already have CDC enabled
        SELECT 1
        FROM cdc.change_tables ct
        WHERE ct.source_object_id = t.object_id
    );

OPEN table_cursor;
FETCH NEXT FROM table_cursor INTO @table_name, @schema_name, @full_table_name;

WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            -- Build the CDC enable command
            SET @sql = 'EXEC sys.sp_cdc_enable_table 
                    @source_schema = ''' + @schema_name + ''',
                    @source_name = ''' + @table_name + ''',
                    @role_name = NULL,
                    @supports_net_changes = 1';

            -- Execute the command
            EXEC sp_executesql @sql;

            PRINT 'CDC enabled for table: ' + @full_table_name;
        END TRY
        BEGIN CATCH
            PRINT 'Failed to enable CDC for table: ' + @full_table_name +
                  ' - Error: ' + ERROR_MESSAGE();
        END CATCH

        FETCH NEXT FROM table_cursor INTO @table_name, @schema_name, @full_table_name;
    END

CLOSE table_cursor;
DEALLOCATE table_cursor;

-- Verify CDC is enabled on tables
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    CASE WHEN ct.object_id IS NOT NULL THEN 'Enabled' ELSE 'Disabled' END AS cdc_status
FROM sys.tables t
         INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
         LEFT JOIN cdc.change_tables ct ON t.object_id = ct.source_object_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;