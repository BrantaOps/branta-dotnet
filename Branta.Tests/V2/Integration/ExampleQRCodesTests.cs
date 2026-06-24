using Branta.Classes;
using Branta.Enums;
using Branta.V2.Classes;
using Branta.V2.Interfaces;
using Branta.V2.Services;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Branta.Tests.V2.Integration;

file sealed class SimpleHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new();
}

file static class ServiceFactory
{
    private static readonly string ApiKey =
        Environment.GetEnvironmentVariable("BRANTA_API_KEY") ?? string.Empty;

    public static IBrantaService Create(BrantaServerBaseUrl baseUrl, PrivacyMode privacy)
    {
        var options = Options.Create(new BrantaClientOptions
        {
            BaseUrl = baseUrl,
            DefaultApiKey = ApiKey,
            Privacy = privacy
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
    private readonly IBrantaService _service =
        ServiceFactory.Create(BrantaServerBaseUrl.Production, PrivacyMode.Loose);

    private const string OnChain =
        "bitcoin:bc1qu3k6geqdjncaarsu2vq56tt8php5vsug9kasmq";

    private const string Lightning =
        "lightning:lnbc17760n1p4r4tqupp5yuapqmxldkc8smuwa6t8shkdg9gezulu0vc7htepfsvweph8kqfsdphgfexzmn5vysygetkv4kx7ur9wgsyc6t8dp6xu6twvusy27rpd4cxcegcqzzsxq97zvuqsp53564rg6w4xjqy7jamcfqxyy83a0j8nzfs0wpevs37t5ln49q6hrs9qxpqysgq47hpqmv34g25le8sceq9jdvul2nz7ucyu0vucv56nlfe40x7n3jsu8duxjrn6tgvdspt872crk9zeatafznm9c57m039z7wyx6g3njsqkchkdh";

    private const string ZkOnChain =
        "bitcoin:bc1q6745z6cy3u0k9nprurh3x804c4r7u3u8vxca2n?branta_id=z15b5EsbP5LHJrFco38%2BFp%2BHVaiopAY676NCKek8e1Q%2B4a370TyYhvloS8uLCUHfJ4CzeI%2FbOFmFDGpAQszB0gu1pJ1HOQ%3D%3D&branta_secret=c6e9eb30-6258-4432-9847-bdcc4fd4b0db";

    private const string ZkLightning =
        "lightning:lnbc17760n1p4r4flypp5k56kq3v2935rl3glkqu9vngfueud2zj87hjcff3t0kn0yrge0pfqdzjgfexzmn5vysz6gzyv4mx2mr0wpjhygzvd9nksarwd9hxwgz6v4ex7gztdehhwmr9v3nk2gz90psk6urvv5cqzzsxq97zvuqsp5hut3t0l0s5mvp9yr06v4253kqtf452z6c65s6g9sga445hc03v6s9qxpqysgqqm430zkk9uymjgvllr3aha88hc6q59etxasfqswn8r8pfm3dstlpp46azv906xtcj3wzprxup5fxn65a5wymt7zzq9sw9qdzx8rgdhcpk80nrg";

    private const string NotFound =
        "bitcoin:bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4";

    [Fact]
    public async Task OnChain_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(OnChain);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task Lightning_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(Lightning);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task ZkOnChain_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(ZkOnChain);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task ZkLightning_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(ZkLightning);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task NotFound_ReturnsEmpty()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(NotFound);
        Assert.Empty(result.Payments);
    }
}

public class ProductionStrictExampleQRCodeTests
{
    private readonly IBrantaService _service =
        ServiceFactory.Create(BrantaServerBaseUrl.Production, PrivacyMode.Strict);

    private const string OnChain =
        "bitcoin:bc1qu3k6geqdjncaarsu2vq56tt8php5vsug9kasmq";

    private const string Lightning =
        "lightning:lnbc17760n1p4r4tqupp5yuapqmxldkc8smuwa6t8shkdg9gezulu0vc7htepfsvweph8kqfsdphgfexzmn5vysygetkv4kx7ur9wgsyc6t8dp6xu6twvusy27rpd4cxcegcqzzsxq97zvuqsp53564rg6w4xjqy7jamcfqxyy83a0j8nzfs0wpevs37t5ln49q6hrs9qxpqysgq47hpqmv34g25le8sceq9jdvul2nz7ucyu0vucv56nlfe40x7n3jsu8duxjrn6tgvdspt872crk9zeatafznm9c57m039z7wyx6g3njsqkchkdh";

    private const string ZkOnChain =
        "bitcoin:bc1q6745z6cy3u0k9nprurh3x804c4r7u3u8vxca2n?branta_id=z15b5EsbP5LHJrFco38%2BFp%2BHVaiopAY676NCKek8e1Q%2B4a370TyYhvloS8uLCUHfJ4CzeI%2FbOFmFDGpAQszB0gu1pJ1HOQ%3D%3D&branta_secret=c6e9eb30-6258-4432-9847-bdcc4fd4b0db";

    private const string ZkLightning =
        "lightning:lnbc17760n1p4r4flypp5k56kq3v2935rl3glkqu9vngfueud2zj87hjcff3t0kn0yrge0pfqdzjgfexzmn5vysz6gzyv4mx2mr0wpjhygzvd9nksarwd9hxwgz6v4ex7gztdehhwmr9v3nk2gz90psk6urvv5cqzzsxq97zvuqsp5hut3t0l0s5mvp9yr06v4253kqtf452z6c65s6g9sga445hc03v6s9qxpqysgqqm430zkk9uymjgvllr3aha88hc6q59etxasfqswn8r8pfm3dstlpp46azv906xtcj3wzprxup5fxn65a5wymt7zzq9sw9qdzx8rgdhcpk80nrg";

    private const string NotFound =
        "bitcoin:bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4";

    [Fact]
    public async Task OnChain_PlainText_ReturnsEmpty()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(OnChain);
        Assert.Empty(result.Payments);
    }

    [Fact]
    public async Task Lightning_PlainText_ReturnsEmpty()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(Lightning);
        Assert.Empty(result.Payments);
    }

    [Fact]
    public async Task ZkOnChain_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(ZkOnChain);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task ZkLightning_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(ZkLightning);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task NotFound_PlainText_ReturnsEmpty()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(NotFound);
        Assert.Empty(result.Payments);
    }
}

public class StagingExampleQRCodeTests
{
    private readonly IBrantaService _service =
        ServiceFactory.Create(BrantaServerBaseUrl.Staging, PrivacyMode.Loose);

    private const string OnChain =
        "bitcoin:bc1qgw3dzmhnyvcswc9r0v0z0ajtp8ulm4nuyeahwr";

    private const string Lightning =
        "lightning:lnbc25830n1p4quq9ppp5zszvpgxtu6uwyur6sf7rayc0meqprqlkv30xjzclh6nzm7gavd8sdzh2d6xzemfdenjqsnjv9h8gcfq95sygetkv4kx7ur9wgsyc6t8dp6xu6twvusy27rpd4cxcefq9pfhgct8d9hxw2gcqzzsxqzursp5fcfx5st7x8rgxra42j47hskmzkcz96mx84xcnvs9lpsmjyzqhw2q9qxpqysgq06lxdc93jjpuqsal9unlfct6wuv0v53yxa8kksl85g3qdw7qks7z9jkq39c6wgzar72luwd38sfj0klyqv0zgns4rq7nafnd8qeuudcqql7at4";

    private const string ZkOnChain =
        "bitcoin:bc1q6745z6cy3u0k9nprurh3x804c4r7u3u8vxca2n?branta_id=z15b5EsbP5LHJrFco38%2BFp%2BHVaiopAY676NCKek8e1Q%2B4a370TyYhvloS8uLCUHfJ4CzeI%2FbOFmFDGpAQszB0gu1pJ1HOQ%3D%3D&branta_secret=c6e9eb30-6258-4432-9847-bdcc4fd4b0db";

    private const string ZkLightning =
        "lightning:lnbc25840n1p4qml83pp5aztzddx4k87m0wkd6wmgxr9753400mcj7sa89sa392krmueqv9qqdz92d6xzemfdenjqsnjv9h8gcfq95s9xarpva5kueeqtf9jqsn0d36zqvf3ypzhsctdwpkx2cqzzsxqzursp5c6dt82gqpn5vucmqtctur0p3cuur6xqgc6348wtz7adtgug9uf2q9qxpqysgq5yt6x946w3664th4h02pug9yhgszpznqyfwzndjk2sxe0878slqkdhgce4mr5ky2ux4gy4yt0vsy536tencls8fvu5wdzyaq548yf4qqu0lyg7";

    private const string NotFound =
        "bitcoin:bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4";

    [Fact]
    public async Task OnChain_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(OnChain);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task Lightning_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(Lightning);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task ZkOnChain_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(ZkOnChain);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task ZkLightning_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(ZkLightning);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task NotFound_ReturnsEmpty()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(NotFound);
        Assert.Empty(result.Payments);
    }
}

public class StagingStrictExampleQRCodeTests
{
    private readonly IBrantaService _service =
        ServiceFactory.Create(BrantaServerBaseUrl.Staging, PrivacyMode.Strict);

    private const string OnChain =
        "bitcoin:bc1qgw3dzmhnyvcswc9r0v0z0ajtp8ulm4nuyeahwr";

    private const string Lightning =
        "lightning:lnbc25830n1p4quq9ppp5zszvpgxtu6uwyur6sf7rayc0meqprqlkv30xjzclh6nzm7gavd8sdzh2d6xzemfdenjqsnjv9h8gcfq95sygetkv4kx7ur9wgsyc6t8dp6xu6twvusy27rpd4cxcefq9pfhgct8d9hxw2gcqzzsxqzursp5fcfx5st7x8rgxra42j47hskmzkcz96mx84xcnvs9lpsmjyzqhw2q9qxpqysgq06lxdc93jjpuqsal9unlfct6wuv0v53yxa8kksl85g3qdw7qks7z9jkq39c6wgzar72luwd38sfj0klyqv0zgns4rq7nafnd8qeuudcqql7at4";

    private const string ZkOnChain =
        "bitcoin:bc1q6745z6cy3u0k9nprurh3x804c4r7u3u8vxca2n?branta_id=z15b5EsbP5LHJrFco38%2BFp%2BHVaiopAY676NCKek8e1Q%2B4a370TyYhvloS8uLCUHfJ4CzeI%2FbOFmFDGpAQszB0gu1pJ1HOQ%3D%3D&branta_secret=c6e9eb30-6258-4432-9847-bdcc4fd4b0db";

    private const string ZkLightning =
        "lightning:lnbc25840n1p4qml83pp5aztzddx4k87m0wkd6wmgxr9753400mcj7sa89sa392krmueqv9qqdz92d6xzemfdenjqsnjv9h8gcfq95s9xarpva5kueeqtf9jqsn0d36zqvf3ypzhsctdwpkx2cqzzsxqzursp5c6dt82gqpn5vucmqtctur0p3cuur6xqgc6348wtz7adtgug9uf2q9qxpqysgq5yt6x946w3664th4h02pug9yhgszpznqyfwzndjk2sxe0878slqkdhgce4mr5ky2ux4gy4yt0vsy536tencls8fvu5wdzyaq548yf4qqu0lyg7";

    private const string NotFound =
        "bitcoin:bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4";

    [Fact]
    public async Task OnChain_PlainText_ReturnsEmpty()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(OnChain);
        Assert.Empty(result.Payments);
    }

    [Fact]
    public async Task Lightning_PlainText_ReturnsEmpty()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(Lightning);
        Assert.Empty(result.Payments);
    }

    [Fact]
    public async Task ZkOnChain_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(ZkOnChain);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task ZkLightning_ReturnsPayment()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(ZkLightning);
        Assert.NotEmpty(result.Payments);
    }

    [Fact]
    public async Task NotFound_PlainText_ReturnsEmpty()
    {
        var result = await _service.GetPaymentsByQrCodeAsync(NotFound);
        Assert.Empty(result.Payments);
    }
}
