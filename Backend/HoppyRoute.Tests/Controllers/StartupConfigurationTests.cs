using FluentAssertions;
using HoppyRoute.Tests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HoppyRoute.Tests.Controllers
{
    /// <summary>Program.cs must refuse to start without a usable Jwt:Secret.</summary>
    public class StartupConfigurationTests
    {
        private static Action StartWithSecret(string secret) => () =>
        {
            using var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(b => b.UseSetting("Jwt:Secret", secret));
            factory.CreateClient();
        };

        private static Exception Root(Exception e)
        {
            while (e.InnerException != null && e is not InvalidOperationException) e = e.InnerException;
            return e;
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Startup_WithoutJwtSecret_FailsFast(string secret)
        {
            var ex = StartWithSecret(secret).Should().Throw<Exception>().Which;
            var root = Root(ex);
            root.Should().BeOfType<InvalidOperationException>();
            root.Message.Should().Contain("Jwt:Secret");
        }

        [Fact]
        public void Startup_WithTooShortJwtSecret_FailsFast()
        {
            var ex = StartWithSecret("test-only-too-short").Should().Throw<Exception>().Which;
            Root(ex).Message.Should().Contain("shorter than 32 characters");
        }

        [Fact]
        public void Startup_WithTestOnlyJwtSecret_Starts()
        {
            using var factory = new HoppyApiFactory();
            var create = () => factory.CreateClient();
            create.Should().NotThrow();
        }
    }
}
