using Azure.AI.OpenAI;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using TokenRateLimiter.Core.Abstractions;
using TokenRateLimiter.Integrations.Extensions;

namespace TokenRateLimiter.Example.Services;

public class BatchProcessingService
{
    private readonly AzureOpenAIClient _azureClient;
    private readonly ITokenRateLimiter _rateLimiter;
    private readonly ITokenEstimator _estimator;
    private readonly ILogger<BatchProcessingService> _logger;

    public BatchProcessingService(
        AzureOpenAIClient azureClient,
        ITokenRateLimiter rateLimiter,
        ITokenEstimator estimator,
        ILogger<BatchProcessingService> logger)
    {
        _azureClient = azureClient;
        _rateLimiter = rateLimiter;
        _estimator = estimator;
        _logger = logger;
    }

    public async Task RunExample()
    {
        Console.WriteLine("🚀 HIGH-VOLUME CONCURRENT PROCESSING EXAMPLE");
        Console.WriteLine("This demonstrates the CORE VALUE of TokenRateLimiter:");
        Console.WriteLine("- Processing many large documents simultaneously");
        Console.WriteLine("- Each request uses significant tokens");
        Console.WriteLine("- Without rate limiting: immediate failures");
        Console.WriteLine("- With TokenRateLimiter: smooth execution respecting limits");
        Console.WriteLine();

        // Simulate real-world scenario: processing multiple large documents
        var documents = CreateLargeDocuments();

        Console.WriteLine($"📊 Starting concurrent processing of {documents.Length} large documents...");
        Console.WriteLine("Each document will use ~5,000-10,000 tokens");
        Console.WriteLine("Total estimated tokens: ~50,000-80,000");
        Console.WriteLine("Without rate limiting, this would likely fail on most Azure OpenAI tiers");
        Console.WriteLine();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // THE CORE FEATURE: Concurrent execution with automatic rate limiting
        var tasks = documents.Select(doc => ProcessLargeDocumentAsync(doc)).ToArray();
        var results = await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Show results
        Console.WriteLine($"✅ Successfully processed {results.Length} documents");
        Console.WriteLine($"⏱️ Total time: {stopwatch.Elapsed:mm\\:ss}");
        Console.WriteLine($"📈 Average time per document: {stopwatch.ElapsedMilliseconds / documents.Length}ms");
        Console.WriteLine();

        foreach (var (document, summary) in results)
        {
            Console.WriteLine($"📄 {document.Title}:");
            Console.WriteLine($"   Summary: {summary[..Math.Min(100, summary.Length)]}...");
        }
    }

    private async Task<(Document Document, string Summary)> ProcessLargeDocumentAsync(Document document)
    {
        ChatMessage[] messages = new ChatMessage[]
        {
            ChatMessage.CreateSystemMessage("You are a document analyst. Provide a comprehensive summary of the document focusing on key insights, main themes, and important details."),
            ChatMessage.CreateUserMessage($"Please analyze and summarize this document:\n\nTitle: {document.Title}\n\nContent: {document.Content}")
        };

        // This is where your library shines: automatic rate limiting for high-token requests
        var completion = await _azureClient
            .GetChatClient("gpt-4o")
            .CompleteChatAsync(messages)
            .WithRateLimit(_rateLimiter, _estimator, messages);

        return (document, completion.Value.Content[0].Text);
    }

    private Document[] CreateLargeDocuments()
    {
        // Simulate large documents that would use significant tokens
        return new[]
        {
            new Document("Market Analysis Report", GenerateLargeContent("market analysis", 800)),
            new Document("Technical Architecture Document", GenerateLargeContent("software architecture", 900)),
            new Document("Financial Strategy Overview", GenerateLargeContent("financial planning", 750)),
            new Document("Product Requirements Specification", GenerateLargeContent("product development", 850)),
            new Document("Risk Assessment Report", GenerateLargeContent("risk management", 700)),
            new Document("Customer Research Findings", GenerateLargeContent("customer insights", 800)),
            new Document("Competitive Analysis", GenerateLargeContent("market competition", 750)),
            new Document("Technology Roadmap", GenerateLargeContent("technology strategy", 900)),
            new Document("Operational Procedures Manual", GenerateLargeContent("business operations", 850)),
            new Document("Legal Compliance Review", GenerateLargeContent("compliance requirements", 700))
        };
    }

    private string GenerateLargeContent(string topic, int words)
    {
        // Generate realistic large content that would consume significant tokens
        var sentences = new[]
        {
            $"This comprehensive analysis of {topic} covers multiple strategic dimensions and operational considerations.",
            $"The research methodology employed for this {topic} study includes quantitative and qualitative assessment techniques.",
            $"Key findings indicate significant opportunities for improvement in {topic} implementation across various organizational levels.",
            $"Stakeholder interviews revealed diverse perspectives on {topic} challenges and potential solutions.",
            $"Data analysis demonstrates clear correlation between {topic} effectiveness and overall business performance metrics.",
            $"Recommendations include both short-term tactical adjustments and long-term strategic initiatives for {topic} optimization.",
            $"Implementation roadmap for {topic} improvements requires coordination across multiple departments and business units.",
            $"Risk mitigation strategies for {topic} initiatives must address both technical and organizational change management aspects.",
            $"Success metrics for {topic} programs should include both quantitative KPIs and qualitative stakeholder satisfaction measures.",
            $"Continuous monitoring and adaptation of {topic} strategies will be essential for sustained competitive advantage."
        };

        var content = new List<string>();
        var random = new Random();

        for (int i = 0; i < words / 25; i++) // Roughly 25 words per sentence
        {
            content.Add(sentences[random.Next(sentences.Length)]);
        }

        return string.Join(" ", content);
    }

    private record Document(string Title, string Content);
}
