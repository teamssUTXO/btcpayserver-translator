using BTCPayTranslator.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;
using Xunit;

namespace BTCPayTranslator.Tests.Services;

public class TranslationExtractorTests
{
    private static TranslationExtractor CreateSut()
    {
        return new TranslationExtractor(NullLogger<TranslationExtractor>.Instance, new HttpClient());
    }

    private static TranslationExtractor CreateSut(HttpClient httpClient)
    {
        return new TranslationExtractor(NullLogger<TranslationExtractor>.Instance, httpClient);
    }

    [Fact]
    public void MergeTranslations_OverridesExistingAndAddsNewKeys()
    {
        var sut = CreateSut();

        var existing = new Dictionary<string, string>
        {
            ["hello"] = "bonjour",
            ["bye"] = "au revoir"
        };
        var incoming = new Dictionary<string, string>
        {
            ["bye"] = "salut",
            ["thanks"] = "merci"
        };

        var merged = sut.MergeTranslations(existing, incoming);

        Assert.Equal(3, merged.Count);
        Assert.Equal("bonjour", merged["hello"]);
        Assert.Equal("salut", merged["bye"]);
        Assert.Equal("merci", merged["thanks"]);
    }

    [Fact]
    public void GetTranslationsToUpdate_ReturnsOnlyMissingKeys()
    {
        var sut = CreateSut();

        var source = new Dictionary<string, string>
        {
            ["hello"] = "Hello",
            ["bye"] = "Goodbye",
            ["thanks"] = "Thanks"
        };
        var existing = new Dictionary<string, string>
        {
            ["hello"] = "Hello",
            ["bye"] = "Old value"
        };

        var toUpdate = sut.GetTranslationsToUpdate(source, existing);

        Assert.Single(toUpdate);
        Assert.Equal("Thanks", toUpdate["thanks"]);
        Assert.False(toUpdate.ContainsKey("hello"));
        Assert.False(toUpdate.ContainsKey("bye"));
    }

    [Fact]
    public void GetTranslationsToUpdate_ReturnsEmpty_WhenAllKeysAlreadyExist()
    {
        var sut = CreateSut();

        var source = new Dictionary<string, string>
        {
            ["hello"] = "Hello",
            ["bye"] = "Goodbye"
        };
        var existing = new Dictionary<string, string>
        {
            ["hello"] = "Hello",
            ["bye"] = "Goodbye"
        };

        var toUpdate = sut.GetTranslationsToUpdate(source, existing);

        Assert.Empty(toUpdate);
    }

    [Fact]
    public async Task ExtractFromBTCPayServerAsync_ReplacesEmptyValuesWithKeys()
    {
        var handler = new QueueHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"hello\":\"Hello\",\"bye\":\"\"}", Encoding.UTF8, "application/json")
        });
        var sut = CreateSut(new HttpClient(handler));

        var result = await sut.ExtractFromBTCPayServerAsync("https://btcpay.test");

        Assert.Equal("Hello", result["hello"]);
        Assert.Equal("bye", result["bye"]);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task ExtractFromDefaultFileAsync_ParsesKnownTranslationsBlock()
    {
        var sut = CreateSut();
        var tempFile = Path.GetTempFileName();

        try
        {
            var content = "public class Seed\n" +
                          "{\n" +
                          "    public void Load()\n" +
                          "    {\n" +
                          "        var knownTranslations = \"\"\"\n" +
                          "{\n" +
                          "  \"hello\": \"Hello\",\n" +
                          "  \"bye\": \"\"\n" +
                          "}\n" +
                          "\"\"\";\n" +
                          "    }\n" +
                          "}\n";

            await File.WriteAllTextAsync(tempFile, content);

            var result = await sut.ExtractFromDefaultFileAsync(tempFile);

            Assert.Equal("Hello", result["hello"]);
            Assert.Equal("bye", result["bye"]);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task LoadExistingTranslationsAsync_SkipsMetadataAndEmptyValues()
    {
        var sut = CreateSut();
        var tempFile = Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(tempFile, """
                {
                  "NOTICE_WARN": "warn",
                  "code": "fr-FR",
                  "currentLanguage": "French",
                  "hello": "bonjour",
                  "empty": ""
                }
                """);

            var result = await sut.LoadExistingTranslationsAsync(tempFile);

            Assert.Single(result);
            Assert.Equal("bonjour", result["hello"]);
            Assert.False(result.ContainsKey("NOTICE_WARN"));
            Assert.False(result.ContainsKey("empty"));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
