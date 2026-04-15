using BTCPayTranslator.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BTCPayTranslator.Tests;

public class TranslationExtractorTests
{
    [Fact]
    public void MergeTranslations_OverridesExistingAndAddsNewKeys()
    {
        var sut = new TranslationExtractor(NullLogger<TranslationExtractor>.Instance, new HttpClient());

        var existing = new Dictionary<string, string>
        {
            ["hello"] = "Hello",
            ["bye"] = "Bye"
        };
        var incoming = new Dictionary<string, string>
        {
            ["bye"] = "Goodbye",
            ["thanks"] = "Thanks"
        };

        var merged = sut.MergeTranslations(existing, incoming);

        Assert.Equal(3, merged.Count);
        Assert.Equal("Hello", merged["hello"]);
        Assert.Equal("Goodbye", merged["bye"]);
        Assert.Equal("Thanks", merged["thanks"]);
    }
}
