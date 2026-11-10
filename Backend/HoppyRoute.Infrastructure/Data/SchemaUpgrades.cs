using Microsoft.EntityFrameworkCore;

namespace HoppyRoute.Infrastructure.Data
{
    /// <summary>
    /// The database is created with EnsureCreated, which never alters an existing database, so a column
    /// added to the model later is missing from databases that already exist, and every query on that table
    /// then fails with "Invalid column name". Each step here is idempotent and runs at startup right after
    /// EnsureCreated. SQL Server only; other providers (the in-memory test provider) build the whole model fresh.
    /// </summary>
    public static class SchemaUpgrades
    {
        private const string SqlServerProvider = "Microsoft.EntityFrameworkCore.SqlServer";

        /// <summary>RouteStops.SkipReason nvarchar(500) NULL.</summary>
        public const string AddRouteStopSkipReasonSql =
            "IF OBJECT_ID(N'[dbo].[RouteStops]', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.RouteStops', N'SkipReason') IS NULL " +
            "ALTER TABLE [dbo].[RouteStops] ADD [SkipReason] nvarchar(500) NULL";

        public static void Apply(HoppyDbContext context)
        {
            if (context.Database.ProviderName != SqlServerProvider)
                return;

            context.Database.ExecuteSqlRaw(AddRouteStopSkipReasonSql);
        }
    }
}
