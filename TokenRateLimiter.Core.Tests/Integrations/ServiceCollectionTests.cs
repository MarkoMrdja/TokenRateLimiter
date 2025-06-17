using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Integrations.Extensions;

namespace TokenRateLimiter.Core.Tests.Integrations;

public class ServiceCollectionTests
{
    [Fact]
    public void AddAzureOpenAIRateLimiting_ShouldRegisterRequiredServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAzureOpenAIRateLimiting(options =>
        {
            options.TokensPerMinute = 500_000;
            options.SafetyBuffer = 25_000;
        });

        var serviceProvider = services.BuildServiceProvider();

        // Assert - Just verify services are registered
        serviceProvider.GetService<ITokenRateLimiter>().Should().NotBeNull();
        serviceProvider.GetService<ITokenEstimator>().Should().NotBeNull();
    }

    [Fact]
    public void AddAzureOpenAIRateLimiting_WithDefaults_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddAzureOpenAIRateLimiting(); // No configuration

        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<ITokenRateLimiter>().Should().NotBeNull();
        serviceProvider.GetService<ITokenEstimator>().Should().NotBeNull();
    }

    [Fact]
    public void AddAzureOpenAIRateLimiting_ShouldRegisterServicesAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAzureOpenAIRateLimiting();

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var rateLimiter1 = serviceProvider.GetService<ITokenRateLimiter>();
        var rateLimiter2 = serviceProvider.GetService<ITokenRateLimiter>();

        // Assert
        rateLimiter1.Should().BeSameAs(rateLimiter2);
    }
}