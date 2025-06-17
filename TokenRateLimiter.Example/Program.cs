using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TokenRateLimiter.Example.Services;
using TokenRateLimiter.Integrations.Extensions;

namespace TokenRateLimiter.Example;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 TokenRateLimiter Example Application");
        Console.WriteLine("This example demonstrates rate-limited LLM calls with automatic token management.\n");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .AddUserSecrets<Program>() // For secure API key storage
            .Build();

        // Build host with dependency injection
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Configure logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Get Azure OpenAI configuration
                var azureConfig = configuration.GetSection("AzureOpenAI");
                var endpoint = azureConfig["Endpoint"];
                var apiKey = azureConfig["ApiKey"];
                var model = azureConfig["Model"] ?? "gpt-4o";
                var tokensPerMinute = azureConfig.GetValue<int>("TokensPerMinute", 1_000_000);

                // Validate configuration
                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("❌ Azure OpenAI configuration is missing!");
                    Console.WriteLine("Please update appsettings.json with your Azure OpenAI endpoint and API key.");
                    Console.WriteLine("You can also use user secrets: dotnet user-secrets set \"AzureOpenAI:ApiKey\" \"your-key\"");
                    Environment.Exit(1);
                }

                // Add Azure OpenAI client
                services.AddSingleton(provider =>
                    new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)));

                // Add rate limiting for Azure OpenAI
                services.AddAzureOpenAIRateLimiting(options =>
                {
                    options.TokensPerMinute = tokensPerMinute;
                    options.SafetyBuffer = 50_000;
                    options.MinWaitTimeMs = 3_000;
                    options.MaxWaitTimeMs = 30_000;
                });

                // Add example services
                services.AddScoped<ChatExampleService>();
                services.AddScoped<BatchProcessingService>();
                services.AddScoped<ManualReservationService>();
                services.AddScoped<MonitoringService>();
            })
            .Build();

        try
        {
            await RunExamplesAsync(host.Services);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }

        Console.WriteLine("\n✅ Example completed. Press any key to exit...");
        Console.ReadKey();
    }

    static async Task RunExamplesAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        Console.WriteLine("Choose an example to run:");
        Console.WriteLine("1. 📝 Simple integration - Add rate limiting with one line");
        Console.WriteLine("2. 🚀 High-volume processing - Concurrent large document analysis");
        Console.WriteLine("3. ⚙️ Manual reservations - Advanced control with fallback strategies");
        Console.WriteLine("4. 📊 Monitoring - Usage statistics and capacity planning");
        Console.WriteLine("5. 🎯 Run all examples");
        Console.Write("\nEnter your choice (1-5): ");

        var choice = Console.ReadLine();

        switch (choice)
        {
            case "1":
                await RunChatExample(scope.ServiceProvider);
                break;
            case "2":
                await RunBatchExample(scope.ServiceProvider);
                break;
            case "3":
                await RunManualExample(scope.ServiceProvider);
                break;
            case "4":
                await RunMonitoringExample(scope.ServiceProvider);
                break;
            case "5":
                await RunAllExamples(scope.ServiceProvider);
                break;
            default:
                Console.WriteLine("Invalid choice. Running simple integration example...");
                await RunChatExample(scope.ServiceProvider);
                break;
        }
    }

    static async Task RunAllExamples(IServiceProvider services)
    {
        await RunChatExample(services);
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        await RunBatchExample(services);
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        await RunManualExample(services);
        Console.WriteLine("\n" + new string('-', 60) + "\n");

        await RunMonitoringExample(services);
    }

    static async Task RunChatExample(IServiceProvider services)
    {
        Console.WriteLine("📝 Example 1: Simple chat with automatic rate limiting");
        var chatService = services.GetRequiredService<ChatExampleService>();
        await chatService.RunExample();
    }

    static async Task RunBatchExample(IServiceProvider services)
    {
        Console.WriteLine("🔄 Example 2: Batch processing with rate limiting");
        var batchService = services.GetRequiredService<BatchProcessingService>();
        await batchService.RunExample();
    }

    static async Task RunManualExample(IServiceProvider services)
    {
        Console.WriteLine("⚙️ Example 3: Manual reservation for advanced control");
        var manualService = services.GetRequiredService<ManualReservationService>();
        await manualService.RunExample();
    }

    static async Task RunMonitoringExample(IServiceProvider services)
    {
        Console.WriteLine("📊 Example 4: Monitoring and statistics");
        var monitoringService = services.GetRequiredService<MonitoringService>();
        await monitoringService.RunExample();
    }
}