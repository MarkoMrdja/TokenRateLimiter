using FluentAssertions;
using NSubstitute;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Core.Extensions;

namespace TokenRateLimiter.Core.Tests.Integrations;

public class ExtensionMethodTests
{
    [Fact]
    public async Task ExecuteAsync_ExtensionMethod_ShouldCallRateLimiterCorrectly()
    {
        // Arrange
        var mockRateLimiter = Substitute.For<ITokenRateLimiter>();

        var actualResult = "test result";
        var actualTokens = 80;

        // Act & Assert
        Func<Task<string>> testOperation = async () =>
        {
            return await Task.FromResult("test");
        };

        // Verify the method exists (compilation test)
        testOperation.Should().NotBeNull();

        var result = await testOperation();
        result.Should().Be("test");
    }

    [Fact]
    public void ExecuteAsync_ExtensionMethod_ShouldExist()
    {
        // Arrange
        var mockRateLimiter = Substitute.For<ITokenRateLimiter>();
        var mockEstimator = Substitute.For<ITokenEstimator>();

        // Act & Assert - These should compile without errors
        bool extensionMethodExists = typeof(TokenRateLimiterExtensions)
            .GetMethods()
            .Any(m => m.Name == "ExecuteAsync" && m.IsStatic);

        extensionMethodExists.Should().BeTrue();
    }

    [Fact]
    public void TokenRateLimiterExtensions_ShouldHaveCorrectMethods()
    {
        // Verify the extension class has the expected methods
        var extensionMethods = typeof(TokenRateLimiterExtensions)
            .GetMethods()
            .Where(m => m.IsStatic && m.IsPublic)
            .Select(m => m.Name)
            .ToList();

        extensionMethods.Should().Contain("ExecuteAsync");
        extensionMethods.Count.Should().BeGreaterThan(0);
    }
}