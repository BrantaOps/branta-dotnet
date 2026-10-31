using Branta.Classes;
using Branta.Enums;
using Branta.V2.Interfaces;
using Branta.V2.Models;
using Branta.V2.Services;
using Microsoft.Extensions.Options;

namespace Branta.Tests.V2.Integration;

file sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("BRANTA_RUN_INTEGRATION") != "1")
            Skip = "Live lookups skipped unless BRANTA_RUN_INTEGRATION=1";
    }
}

file sealed class SimpleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient();
        var token = Environment.GetEnvironmentVariable("BRANTA_INTEGRATION_METRICS_TOKEN");
        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Branta-Integration-Token", token);
        return client;
    }
}

file static class ServiceFactory
{
    private static readonly string ApiKey =
        Environment.GetEnvironmentVariable("BRANTA_API_KEY") ?? string.Empty;

    public static IBrantaService Create(BrantaEnvironment environment)
    {
        var options = Options.Create(new BrantaApiOptions
        {
            Environment = environment,
            ApiKey = ApiKey
        });
        return new BrantaService(
            new BrantaClient(new SimpleHttpClientFactory(), options),
            new AesEncryptionService(),
            options,
            new GuidSecretGenerator());
    }
}

public class ProductionExampleQRCodeTests
{
    private readonly IBrantaService _service = ServiceFactory.Create(BrantaEnvironment.Production);

    private const string OnChain =
        "bitcoin:bc1qu3k6geqdjncaarsu2vq56tt8php5vsug9kasmq";

    private const string Lightning =
        "lightning:lnbc17760n1p4r4tqupp5yuapqmxldkc8smuwa6t8shkdg9gezulu0vc7htepfsvweph8kqfsdphgfexzmn5vysygetkv4kx7ur9wgsyc6t8dp6xu6twvusy27rpd4cxcegcqzzsxq97zvuqsp53564rg6w4xjqy7jamcfqxyy83a0j8nzfs0wpevs37t5ln49q6hrs9qxpqysgq47hpqmv34g25le8sceq9jdvul2nz7ucyu0vucv56nlfe40x7n3jsu8duxjrn6tgvdspt872crk9zeatafznm9c57m039z7wyx6g3njsqkchkdh";

    private const string BrantaOnChain =
        "bitcoin:bc1q6745z6cy3u0k9nprurh3x804c4r7u3u8vxca2n?branta_id=Qy4e3KJ9yRs%2BVWq59aSTC6o%2F6vbCnw0v7ZutEs1mxjpGM2i5DgVVl7G0TwFO9CqQ1ws3zNqogQu%2B2%2Bx3U%2BQ8%2FSZtwgjezw%3D%3D&branta_secret=f9b0fd20-b402-4cd2-b68f-3ae3db52dbf5";

    private const string BrantaLightning =
        "lightning:lnbc17760n1p4r4flypp5k56kq3v2935rl3glkqu9vngfueud2zj87hjcff3t0kn0yrge0pfqdzjgfexzmn5vysz6gzyv4mx2mr0wpjhygzvd9nksarwd9hxwgz6v4ex7gztdehhwmr9v3nk2gz90psk6urvv5cqzzsxq97zvuqsp5hut3t0l0s5mvp9yr06v4253kqtf452z6c65s6g9sga445hc03v6s9qxpqysgqqm430zkk9uymjgvllr3aha88hc6q59etxasfqswn8r8pfm3dstlpp46azv906xtcj3wzprxup5fxn65a5wymt7zzq9sw9qdzx8rgdhcpk80nrg";

    private const string BrantaUnified =
        "bitcoin:bc1qu3k6geqdjncaarsu2vq56tt8php5vsug9kasmq?lightning=lnbc17760n1p4r4tqupp5yuapqmxldkc8smuwa6t8shkdg9gezulu0vc7htepfsvweph8kqfsdphgfexzmn5vysygetkv4kx7ur9wgsyc6t8dp6xu6twvusy27rpd4cxcegcqzzsxq97zvuqsp53564rg6w4xjqy7jamcfqxyy83a0j8nzfs0wpevs37t5ln49q6hrs9qxpqysgq47hpqmv34g25le8sceq9jdvul2nz7ucyu0vucv56nlfe40x7n3jsu8duxjrn6tgvdspt872crk9zeatafznm9c57m039z7wyx6g3njsqkchkdh&branta_id=%2BbcErHkjD6aDfvhegd1CPRgQQwes64aMIqPXCRBVKP88d982zJt7TUHTq%2B11O9ij9HFzXC%2BTvh%2ByFoECrvZKpv%2FVuGGLIA%3D%3D&branta_secret=c4ba0501-059e-459b-9b11-64d97f158291";

    private const string NotFound =
        "bitcoin:bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4";

    [LiveFact]
    public async Task OnChain_ReturnsUnknown()
    {
        var result = await _service.VerifyAsync(OnChain);
        Assert.Equal(VerifyStatus.Unknown, result.Status);
    }

    [LiveFact]
    public async Task Lightning_ReturnsMatch()
    {
        var result = await _service.VerifyAsync(Lightning);
        Assert.Equal(VerifyStatus.Match, result.Status);
    }

    [LiveFact]
    public async Task BrantaOnChain_ReturnsMatch()
    {
        var result = await _service.VerifyAsync(BrantaOnChain);
        Assert.Equal(VerifyStatus.Match, result.Status);
    }

    [LiveFact]
    public async Task BrantaLightning_ReturnsMatch()
    {
        var result = await _service.VerifyAsync(BrantaLightning);
        Assert.Equal(VerifyStatus.Match, result.Status);
    }

    [LiveFact]
    public async Task BrantaUnified_ReturnsMatch()
    {
        var result = await _service.VerifyAsync(BrantaUnified);
        Assert.Equal(VerifyStatus.Match, result.Status);
    }

    [LiveFact]
    public async Task NotFound_ReturnsUnknown()
    {
        var result = await _service.VerifyAsync(NotFound);
        Assert.Equal(VerifyStatus.Unknown, result.Status);
    }
}

public class StagingExampleQRCodeTests
{
    private readonly IBrantaService _service = ServiceFactory.Create(BrantaEnvironment.Staging);

    private const string OnChain =
        "bitcoin:bc1qgw3dzmhnyvcswc9r0v0z0ajtp8ulm4nuyeahwr";

    private const string Bolt11 =
        "lightning:lnbc10u1p4w23tzpp50uc6g4020tt3ffp5xfpp83k0lcv2spytpymuupqrnsj0eth8jqgsdqhgfexzmn5vys8xarpva5kuecxq97zvuqsp5tj2a5fd84lhdddzfqz2lwmjqg7n9gp28yy5vtqd78muyzdt5tmsq9qypsqqcqpf6yv66fkcq609xzk0fcqx82hd7fsjhwrzj8xmq743v5lrtssrj95r94hyeumqeejgqvtxqy7ks7ec0uc5lc5zxg240xtwgzjtwkmh3aqqm8gyny";

    private const string BrantaOnChain =
        "bitcoin:bc1qgw3dzmhnyvcswc9r0v0z0ajtp8ulm4nuyeahwr?branta_id=6SpYCe23r79x0t8rNvmjWqhrg8hen4xz%2FYSXhNJ9HLSYrTRPnm%2BSwAuf6NOKobnwE%2B3MQBNkyTnCtIWpKKX2E9ZUWVedig%3D%3D&branta_secret=05eb145d-b5cb-452a-9256-0bdfd9a15ffa";

    private const string Ark =
        "ark1qztwuhx7vpytv4dcfeqjsepx3kxstnarelmry2smrfdgt2dv87fas7gtqkk";

    private const string SilentPayment =
        "sp1qqgste7k9hx0qftg6qmwlkqtwuy6cycyavzmzj85c6qdfhjdpdjtdgqjuexzk6murw56suy3e0rd2cgqvycxttddwsvgxe2usfpxumr70xc9pkqwv";

    private const string BrantaUnified =
        "bitcoin:bc1q6745z6cy3u0k9nprurh3x804c4r7u3u8vxca2n?lightning=lnbc10u1p4w23tzpp5s6jc8gvun27un7g7q9kmh2vak78r83766heesd9fepa5s56hrpsqdqhgfexzmn5vys8xarpva5kuecxq97zvuqsp5gt4k9wwdgf85g066a05zrnlkh3jc2ng3mh9mw6yw7z03l4kmqzys9qypsqqcqpf7pk6xjfksy8sqhv24es2qgtp0e88wvt2gdv2zceealhjwh0memhzktgnfhdvzc9a6kt74w5jf5c8v9t20k38snawrgngr0sjw9a58fqqn9lwtq&branta_id=NxkrlFgIvGg6f8YZmFAf8qTUza8nXfn%2Bb9H3W7czFAo5Lp4cSpgRSmJgx7P9vaOYu7lkP6XXKvTshQfkXxxRrxJ%2BE7zdpA%3D%3D&branta_secret=6acf2637-511a-4b89-ba2c-f5914d7e669c";

    private const string NotFound =
        "bitcoin:bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4";

    [LiveFact]
    public async Task OnChain_ReturnsUnknown()
    {
        var result = await _service.VerifyAsync(OnChain);
        Assert.Equal(VerifyStatus.Unknown, result.Status);
    }

    [LiveFact]
    public async Task Bolt11_ReturnsMatch()
    {
        var result = await _service.VerifyAsync(Bolt11);
        Assert.Equal(VerifyStatus.Match, result.Status);
    }

    [LiveFact]
    public async Task BrantaOnChain_ReturnsMatch()
    {
        var result = await _service.VerifyAsync(BrantaOnChain);
        Assert.Equal(VerifyStatus.Match, result.Status);
    }

    [LiveFact]
    public async Task Ark_ReturnsMatch()
    {
        var result = await _service.VerifyAsync(Ark);
        Assert.Equal(VerifyStatus.Match, result.Status);
    }

    [LiveFact]
    public async Task SilentPayment_ReturnsMatch()
    {
        var result = await _service.VerifyAsync(SilentPayment);
        Assert.Equal(VerifyStatus.Match, result.Status);
    }

    [LiveFact]
    public async Task BrantaUnified_ReturnsMatch()
    {
        var result = await _service.VerifyAsync(BrantaUnified);
        Assert.Equal(VerifyStatus.Match, result.Status);
    }

    [LiveFact]
    public async Task NotFound_ReturnsUnknown()
    {
        var result = await _service.VerifyAsync(NotFound);
        Assert.Equal(VerifyStatus.Unknown, result.Status);
    }
}
