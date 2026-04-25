using BTCPayTranslator.Models;
using BTCPayTranslator.Services;

namespace BTCPayTranslator.Tests;

internal sealed class FakeTranslationService : ITranslationService
{
    private readonly Func<TranslationRequest, TranslationResponse> _translate;

    public FakeTranslationService(Func<TranslationRequest, TranslationResponse>? translate = null)
    {
        _translate = translate ?? (r => new TranslationResponse(r.Key, $"translated-{r.Key}", true));
    }

    public string ProviderName => "Fake";

    public List<TranslationRequest> SeenRequests { get; } = new();

    public Task<TranslationResponse> TranslateAsync(TranslationRequest request)
    {
        SeenRequests.Add(request);
        return Task.FromResult(_translate(request));
    }

    public Task<BatchTranslationResponse> TranslateBatchAsync(BatchTranslationRequest request)
    {
        SeenRequests.AddRange(request.Items);
        var results = request.Items
            .Select(_translate)
            .ToList();

        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count - successCount;

        return Task.FromResult(new BatchTranslationResponse(results, successCount, failureCount, TimeSpan.Zero));
    }
}
