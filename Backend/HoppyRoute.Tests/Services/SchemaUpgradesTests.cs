using FluentAssertions;
using HoppyRoute.Infrastructure.Data;
using HoppyRoute.Tests.TestSupport;

namespace HoppyRoute.Tests.Services
{
    public class SchemaUpgradesTests
    {
        [Fact]
        public void Apply_IsANoOp_OnTheInMemoryProvider()
        {
            // The in-memory provider builds the whole model fresh (SkipReason included); running the
            // SQL Server step against it must neither throw nor try to execute SQL.
            using var db = TestDb.Create();
            var act = () => SchemaUpgrades.Apply(db);
            act.Should().NotThrow();
        }

        [Fact]
        public void SkipReasonStep_IsGuardedSoItCanRunOnEveryStartup()
        {
            var sql = SchemaUpgrades.AddRouteStopSkipReasonSql;
            sql.Should().Contain("COL_LENGTH(N'dbo.RouteStops', N'SkipReason') IS NULL");
            sql.Should().Contain("OBJECT_ID(N'[dbo].[RouteStops]', N'U') IS NOT NULL");
            sql.Should().Contain("ADD [SkipReason] nvarchar(500) NULL");
        }

        [Fact]
        public void SkipReason_IsMappedWithTheSameLengthAsTheStep()
        {
            using var db = TestDb.Create();
            var property = db.Model.FindEntityType(typeof(HoppyRoute.Domain.Entities.RouteStop))!.FindProperty("SkipReason")!;
            property.GetMaxLength().Should().Be(500);
            property.IsNullable.Should().BeTrue();
        }
    }
}
