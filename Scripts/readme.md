# TbdDevelop.CDC.SqlServer

A .NET library for monitoring SQL Server Change Data Capture (CDC) changes and publishing them to configured handlers.

## Getting Started

### Prerequisites

- SQL Server with CDC enabled
- .NET 9.0 or later
- Appropriate database permissions (sysadmin for CDC setup)

### Database Setup

The library requires CDC to be enabled on your database and tables. Run the following scripts in order:

#### 1. Enable CDC on All Tables (Required - May Need Customization)

Run `001_enable_cdc_all_tables.sql` to enable CDC on your database and all user tables.

**⚠️ Important**: This script enables CDC on **ALL** user tables across **ALL** schemas in the database. You may want to modify this script to:
- Target specific schemas only
- Exclude certain tables
- Use different role names for CDC access

The script will:
- Enable CDC at the database level (requires sysadmin permissions)
- Automatically enable CDC on all user tables that don't already have it
- Skip system tables and tables that already have CDC enabled
- Print success/failure messages for each table

#### 2. Create Checkpoint Table (Required)

Run `002_add_cdc_checkpoints.sql` to create the checkpoint tracking table.

This script creates the `cdc.CdcCheckpoints` table which is **essential** for the library to function properly. It allows the monitoring service to:
- Track the last processed Log Sequence Number (LSN) for each table
- Resume from the correct position after application restarts
- Avoid reprocessing changes that have already been published

### Library Configuration

#### 1. Install the Package

Add the library to your project:

```bash
dotnet add package TbdDevelop.CDC.SqlServer
```

#### 2. Configure Services

In your `Program.cs` or startup configuration:

```csharp
using TbdDevelop.CDC.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Add CDC monitoring with change handlers
builder.AddChangeMonitoring(config =>
{
    // Configure your change handlers here
    // See change handling section below
});

var host = builder.Build();
host.Run();
```

#### 3. Configuration Settings

Add the following to your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_server;Database=your_database;Integrated Security=true;"
  },
  "monitoring": {
    "connectionStringName": "DefaultConnection",
    "tables": [
      "schema1.table1",
      "schema2.table2",
      "dbo.users"
    ]
  }
}
```

**Configuration Options:**
- `connectionStringName`: Name of the connection string to use from ConnectionStrings section
- `tables`: Array of table names to monitor (format: "schema.tablename")

### Change Handling

The library allows you to configure custom handlers for different types of changes. Use the configuration delegate to set up your handlers:

```csharp
builder.AddChangeMonitoring(config =>
{
    // Configure handlers for specific tables or change types
    // Handler configuration details to be documented based on implementation
});
```

### Running the Application

Once configured, the library will:
1. Monitor the specified tables for changes
2. Track progress using the checkpoint table
3. Publish changes to your configured handlers
4. Resume from the last processed position on restart

### Troubleshooting

**CDC Not Enabled**: Ensure you have sysadmin permissions and CDC is supported on your SQL Server edition.

**Permission Issues**: The application requires appropriate permissions to read CDC tables and update the checkpoint table.

**Missing Checkpoints**: Ensure the `002_add_cdc_checkpoints.sql` script has been run successfully.

### Next Steps

- Configure your change handlers based on your application needs
- Set up appropriate logging and monitoring
- Consider performance implications for high-volume tables
- Test the restart behavior to ensure checkpoints work correctly
