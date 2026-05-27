using Branta.Enums;
using Branta.Extensions;
using System.Text.RegularExpressions;

namespace Branta.Tests.Extensions;

public class BrantaExtensionsTest
{
    // GetUrl

    [Fact]
    public void GetUrl_Localhost()
    {
        Assert.Equal("http://localhost:3000", BrantaServerBaseUrl.Localhost.GetUrl());
    }

    [Fact]
    public void GetUrl_Production()
    {
        Assert.Equal("https://guardrail.branta.pro", BrantaServerBaseUrl.Production.GetUrl());
    }

    [Fact]
    public void GetUrl_Staging()
    {
        Assert.Equal("https://staging.guardrail.branta.pro", BrantaServerBaseUrl.Staging.GetUrl());
    }

    // IsBolt11

    [Fact]
    public void IsBolt11_ReturnsTrue_ForLnbcPrefix()
    {
        Assert.True("lnbc100n1ptest".IsBolt11());
    }

    [Fact]
    public void IsBolt11_ReturnsTrue_ForLntbPrefix()
    {
        Assert.True("lntb100n1ptest".IsBolt11());
    }

    [Fact]
    public void IsBolt11_ReturnsTrue_ForLnbcrtPrefix()
    {
        Assert.True("lnbcrt100n1ptest".IsBolt11());
    }

    [Fact]
    public void IsBolt11_IsCaseInsensitive()
    {
        Assert.True("LNBC100N1PTEST".IsBolt11());
    }

    [Fact]
    public void IsBolt11_ReturnsFalse_ForNonBolt11Values()
    {
        Assert.False("bc1qabc".IsBolt11());
        Assert.False("ark1qqjqtest".IsBolt11());
    }

    // IsArk

    [Fact]
    public void IsArk_ReturnsTrue_ForArk1Prefix()
    {
        Assert.True("ark1qqjqtest".IsArk());
    }

    [Fact]
    public void IsArk_IsCaseInsensitive()
    {
        Assert.True("ARK1QQJQTEST".IsArk());
    }

    [Fact]
    public void IsArk_ReturnsFalse_ForNonArkValues()
    {
        Assert.False("bc1qabc".IsArk());
    }

    // GetHashZkType

    [Fact]
    public void GetHashZkType_ReturnsBolt11_ForBolt11Invoice()
    {
        Assert.Equal(DestinationType.Bolt11, "lnbc100n1ptest".GetHashZkType());
    }

    [Fact]
    public void GetHashZkType_ReturnsArkAddress_ForArkAddress()
    {
        Assert.Equal(DestinationType.ArkAddress, "ark1qqjqtest".GetHashZkType());
    }

    [Fact]
    public void GetHashZkType_ReturnsNull_ForBitcoinAddress()
    {
        Assert.Null("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa".GetHashZkType());
    }

    // ToNormalizedHash

    [Fact]
    public void ToNormalizedHash_Returns64CharUppercaseHex()
    {
        var hash = "lnbc100n1ptest".ToNormalizedHash();
        Assert.Matches(new Regex("^[0-9A-F]{64}$"), hash);
    }

    [Fact]
    public void ToNormalizedHash_IsCaseInsensitive()
    {
        Assert.Equal("lnbc100n1ptest".ToNormalizedHash(), "LNBC100N1PTEST".ToNormalizedHash());
    }

    // ToUrlFragment

    [Fact]
    public void ToUrlFragment_FormatsPairsWithKPrefix()
    {
        var fragment = new Dictionary<string, string> { { "zkId1", "secret1" } }.ToUrlFragment();
        Assert.Equal("#k-zkId1=secret1", fragment);
    }

    [Fact]
    public void ToUrlFragment_JoinsMultiplePairsWithAmpersand()
    {
        var fragment = new Dictionary<string, string> { { "a", "1" }, { "b", "2" } }.ToUrlFragment();
        Assert.Equal("#k-a=1&k-b=2", fragment);
    }
}
