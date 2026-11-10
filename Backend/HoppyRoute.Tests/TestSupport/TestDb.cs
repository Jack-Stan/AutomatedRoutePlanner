using HoppyRoute.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoppyRoute.Tests.TestSupport
{
    /// <summary>A fresh, isolated EF Core InMemory database per call.</summary>
    public static class TestDb
    {
        public static HoppyDbContext Create() =>
            new(new DbContextOptionsBuilder<HoppyDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    public static class TestKeys
    {
        /// <summary>Test-only HS256 signing key. Not a real secret; never used outside tests.</summary>
        public const string JwtSecret = "test-only-signing-key-not-a-real-secret-0123456789";
    }
}
