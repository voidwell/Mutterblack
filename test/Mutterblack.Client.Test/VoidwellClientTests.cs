using System.Net;
using Mutterblack.Client.Models;

namespace Mutterblack.Client.Test;

public class VoidwellClientTests
{
    private const string _characterJson = """
        {"id":"5428","name":"Lampjaw","world":"Emerald","factionId":1,"factionName":"VS","battleRank":"120","kills":"1500","killDeathRatio":1.75,"lastSaved":"2024-01-02T03:04:05Z","outfitAlias":"TAG"}
        """;

    [Theory]
    [InlineData(PlatformType.PC, "pc")]
    [InlineData(PlatformType.PS4EU, "ps4eu")]
    [InlineData(PlatformType.PS4US, "ps4us")]
    public async Task GetPlanetsideCharacterStatsAsync_RequestsCharacterByNameForPlatform(PlatformType platform, string expected)
    {
        var (client, handler) = CreateClient(HttpStatusCode.OK, _characterJson);

        await client.GetPlanetsideCharacterStatsAsync(platform, "Lampjaw");

        var (request, _) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal($"https://api.example.com/ps2/character/byname/Lampjaw?platform={expected}", request.RequestUri!.ToString());
    }

    [Fact]
    public async Task GetPlanetsideCharacterStatsAsync_DeserializesCamelCaseAndQuotedNumbers()
    {
        var (client, _) = CreateClient(HttpStatusCode.OK, _characterJson);

        var result = await client.GetPlanetsideCharacterStatsAsync(PlatformType.PC, "Lampjaw");

        Assert.Equal("5428", result.Id);
        Assert.Equal("Lampjaw", result.Name);
        Assert.Equal("Emerald", result.World);
        Assert.Equal("VS", result.FactionName);
        Assert.Equal(120, result.BattleRank);
        Assert.Equal(1500, result.Kills);
        Assert.Equal(1.75, result.KillDeathRatio);
        Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc), result.LastSaved!.Value.ToUniversalTime());
        Assert.Equal("TAG", result.OutfitAlias);
    }

    [Fact]
    public async Task GetPlanetsideCharacterWeaponStatsAsync_RequestsWeaponForCharacter()
    {
        var (client, handler) = CreateClient(HttpStatusCode.OK, """{"characterName":"Lampjaw","weaponName":"MSW-R","kills":10}""");

        var result = await client.GetPlanetsideCharacterWeaponStatsAsync(PlatformType.PS4US, "Lampjaw", "MSW-R");

        Assert.Equal("https://api.example.com/ps2/character/byname/Lampjaw/weapon/MSW-R?platform=ps4us", handler.Requests.Single().Request.RequestUri!.ToString());
        Assert.Equal("MSW-R", result.WeaponName);
        Assert.Equal(10, result.Kills);
    }

    [Fact]
    public async Task GetPlanetsideOutfitStatsAsync_RequestsOutfitByAlias()
    {
        var (client, handler) = CreateClient(HttpStatusCode.OK, """{"name":"The Outfit","alias":"TAG","memberCount":42}""");

        var result = await client.GetPlanetsideOutfitStatsAsync(PlatformType.PC, "TAG");

        Assert.Equal("https://api.example.com/ps2/outfit/byalias/TAG?platform=pc", handler.Requests.Single().Request.RequestUri!.ToString());
        Assert.Equal("The Outfit", result.Name);
        Assert.Equal(42, result.MemberCount);
    }

    [Fact]
    public async Task GetPlanetsideWeaponStatsAsync_RequestsWeaponByName()
    {
        var (client, handler) = CreateClient(HttpStatusCode.OK, """{"name":"MSW-R","hipAcc":{"standing":0.5},"fireModes":["Semi","Auto"]}""");

        var result = await client.GetPlanetsideWeaponStatsAsync("msw");

        Assert.Equal("https://api.example.com/ps2/weaponinfo/byname/msw", handler.Requests.Single().Request.RequestUri!.ToString());
        Assert.Equal("MSW-R", result.Name);
        Assert.Equal(0.5f, result.HipAcc.Standing);
        Assert.Equal(["Semi", "Auto"], result.FireModes);
    }

    [Fact]
    public async Task Requests_ThrowVoidwellExceptionWithReasonWhenApiFails()
    {
        var (client, _) = CreateClient(HttpStatusCode.NotFound, "");

        var ex = await Assert.ThrowsAsync<VoidwellException>(() => client.GetPlanetsideCharacterStatsAsync(PlatformType.PC, "Nobody"));

        Assert.Equal("Not Found", ex.Message);
    }

    [Fact]
    public async Task Requests_AreCaseInsensitiveAboutPropertyNames()
    {
        var (client, _) = CreateClient(HttpStatusCode.OK, """{"NAME":"MSW-R"}""");

        var result = await client.GetPlanetsideWeaponStatsAsync("msw");

        Assert.Equal("MSW-R", result.Name);
    }

    private static (VoidwellClient Client, StubHttpMessageHandler Handler) CreateClient(HttpStatusCode status, string body)
    {
        var handler = new StubHttpMessageHandler(status, body);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com/") };

        return (new VoidwellClient(httpClient), handler);
    }
}
