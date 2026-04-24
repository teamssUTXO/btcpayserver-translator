using BTCPayTranslator.Models;
using Xunit;

namespace BTCPayTranslator.Tests.Models;

public class SupportedLanguagesTests
{
    [Fact]
    public void GetLanguageInfo_ReturnsLanguage_WhenCodeExists()
    {
        var result = SupportedLanguages.GetLanguageInfo("es");

        Assert.NotNull(result);
        Assert.Equal("Spanish", result.Name);
        Assert.Equal("es-ES", result.Code);
    }

    [Fact]
    public void GetLanguageInfo_ReturnsNull_WhenCodeDoesNotExist()
    {
        var result = SupportedLanguages.GetLanguageInfo("does-not-exist");

        Assert.Null(result);
    }

    [Fact]
    public void GetAllLanguages_ReturnsConsistentAndValidLanguageCatalog()
    {
        var all = SupportedLanguages.GetAllLanguages().ToList();

        Assert.NotEmpty(all);
        
        Assert.Equal(SupportedLanguages.Languages.Count, all.Count);
        
        Assert.Contains(all, l => l.Code == "es-ES" && l.Name == "Spanish");
        Assert.Contains(all, l => l.Code == "fr-FR" && l.Name == "French");
        Assert.Contains(all, l => l.Code == "ar" && l.IsRightToLeft);
        
        Assert.All(all, l => Assert.False(string.IsNullOrWhiteSpace(l.Code)));
        Assert.All(all, l => Assert.False(string.IsNullOrWhiteSpace(l.Name)));
        Assert.All(all, l => Assert.False(string.IsNullOrWhiteSpace(l.NativeName)));
        
        Assert.Equal(all.Count, all.Select(l => l.Code).Distinct().Count());
        Assert.Equal(all.Count, all.Select(l => l.Name).Distinct().Count());
    }
}
