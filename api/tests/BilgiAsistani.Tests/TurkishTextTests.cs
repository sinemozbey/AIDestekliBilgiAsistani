using BilgiAsistani.Core;

namespace BilgiAsistani.Tests;

public class TurkishTextTests
{
    [Fact]
    public void Normalize_UsesTurkishCasingForDottedAndDotlessI() =>
        Assert.Equal("iade ışık", TurkishText.Normalize("İADE IŞIK"));

    [Fact]
    public void Tokenize_RemovesStopwordsAndStems() =>
        Assert.Equal(["iate", "süres"], TurkishText.Tokenize("İade süresi ne kadar?"));

    [Fact]
    public void Stem_MapsConsonantSofteningToSameRoot() =>
        Assert.Equal(TurkishText.Tokenize("hesap"), TurkishText.Tokenize("hesabımı"));

    [Theory]
    [InlineData("iadesinde", "iade", true)]
    [InlineData("kargo", "kargoya", true)]
    [InlineData("siler", "silme", false)] // ortak önek 4 karakterden kısa
    public void StemsMatch_UsesPrefixForShortRoots(string a, string b, bool expected) =>
        Assert.Equal(expected, TurkishText.StemsMatch(TurkishText.Tokenize(a)[0], TurkishText.Tokenize(b)[0]));

    [Fact]
    public void SplitSentences_SplitsOnlyBeforeUppercaseOrDigit() =>
        Assert.Equal(["Kurulum için 2.4 GHz gerekir.", "Sonra eşleştirin."],
            TurkishText.SplitSentences("Kurulum için 2.4 GHz gerekir. Sonra eşleştirin."));
}
