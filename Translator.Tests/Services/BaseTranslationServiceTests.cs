using BTCPayTranslator.Models;
using BTCPayTranslator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace BTCPayTranslator.Tests.Services;

public class BaseTranslationServiceTests
{
    [Fact]
    public async Task TranslateAsync_ReturnsTranslatedText_WhenApiReturnsChoices()
    {
        var handler = new QueueHttpMessageHandler(responder: _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "choices": [
                    {
                      "message": {
                        "content": "bonjour"
                      }
                    }
                  ]
                }
                """, Encoding.UTF8, "application/json")
        });

        var fakeTime = new FakeTimeProvider();
        using var service = CreateService(new HttpClient(handler), fakeTime);
        var request = new TranslationRequest("hello", "Hello", "French");

        var result = await service.TranslateAsync(request);

        Assert.True(result.Success);
        Assert.Equal("hello", result.Key);
        Assert.Equal("bonjour", result.TranslatedText);
        Assert.Equal(1, handler.CallCount);

        service.Dispose();
    }

    [Fact]
    public async Task TranslateAsync_ReturnsFailure_WhenApiReturnsNonSuccessStatus()
    {
        var handler = new QueueHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("bad request")
        });

        var fakeTime = new FakeTimeProvider();
        using var service = CreateService(new HttpClient(handler), fakeTime);
        var request = new TranslationRequest("hello", "Hello", "Spanish");

        var translateTask = service.TranslateAsync(request);
        while (!translateTask.IsCompleted)
        {
            await Task.Delay(10);
            fakeTime.Advance(TimeSpan.FromSeconds(1));
        }

        var result = await translateTask;

        Assert.False(result.Success);
        Assert.Contains("API error", result.Error);
        Assert.InRange(handler.CallCount, 2, 3);

        service.Dispose();
    }

    [Fact]
    public async Task TranslateBatchAsync_ReturnsResultForEachInputItem()
    {
        var handler = new QueueHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""
                {
                  "choices": [
                    {
                      "message": {
                        "content": "Translated"
                      }
                    }
                  ]
                }
                """, Encoding.UTF8, "application/json")
        });

        var fakeTime = new FakeTimeProvider();
        using var service = CreateService(new HttpClient(handler), fakeTime);
        var batch = new BatchTranslationRequest(
            new List<TranslationRequest>
            {
                new("k1", "First", "French"),
                new("k2", "Second", "French")
            },
            "French",
            "Français");

        var batchTask = service.TranslateBatchAsync(batch);
        while (!batchTask.IsCompleted)
        {
            await Task.Delay(10);
            fakeTime.Advance(TimeSpan.FromSeconds(1));
        }
        var result = await batchTask;

        Assert.Equal(2, result.Results.Count);
        Assert.Equal(2, result.SuccessCount);
        Assert.Equal(0, result.FailureCount);
        Assert.All(result.Results, r => Assert.True(r.Success));
        Assert.Equal(2, handler.CallCount);

        service.Dispose();
    }

    private static BaseTranslationService CreateService(HttpClient client, TimeProvider? timeProvider = null)
    {
        Environment.SetEnvironmentVariable("OPENROUTER_API_KEY", null);
        Environment.SetEnvironmentVariable("OPENROUTER_MODEL", null);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TranslationService:OpenRouter:ApiKey"] = "test-key",
                ["TranslationService:OpenRouter:Model"] = "test-model"
            })
            .Build();

        return new BaseTranslationService(client, config, NullLogger<BaseTranslationService>.Instance, timeProvider);
    }
}
