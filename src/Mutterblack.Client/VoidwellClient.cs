using System.Text.Json;
using System.Text.Json.Serialization;
using Mutterblack.Client.Models;

namespace Mutterblack.Client;

public class VoidwellClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _serializerOptions;

    public VoidwellClient(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };
    }

    public async Task<SimpleCharacterDetails> GetPlanetsideCharacterStatsAsync(PlatformType platform, string characterName)
    {
        var platformName = platform.ToString().ToLower();
        var url = string.Format("/ps2/character/byname/{0}?platform={1}", characterName, platformName);
        var result = await _httpClient.GetAsync(url);

        if (!result.IsSuccessStatusCode)
        {
            throw new VoidwellException(result.ReasonPhrase);
        }

        return await GetContentAsync<SimpleCharacterDetails>(result);
    }

    public async Task<CharacterWeaponDetails> GetPlanetsideCharacterWeaponStatsAsync(PlatformType platform, string characterName, string weaponName)
    {
        var platformName = platform.ToString().ToLower();
        var url = string.Format("/ps2/character/byname/{0}/weapon/{1}?platform={2}", characterName, weaponName, platformName);
        var result = await _httpClient.GetAsync(url);

        if (!result.IsSuccessStatusCode)
        {
            throw new VoidwellException(result.ReasonPhrase);
        }

        return await GetContentAsync<CharacterWeaponDetails>(result);
    }

    public async Task<OutfitDetails> GetPlanetsideOutfitStatsAsync(PlatformType platform, string outfitAlias)
    {
        var platformName = platform.ToString().ToLower();
        var url = string.Format("/ps2/outfit/byalias/{0}?platform={1}", outfitAlias, platformName);
        var result = await _httpClient.GetAsync(url);

        if (!result.IsSuccessStatusCode)
        {
            throw new VoidwellException(result.ReasonPhrase);
        }

        return await GetContentAsync<OutfitDetails>(result);
    }

    public async Task<WeaponInfoResult> GetPlanetsideWeaponStatsAsync(string weaponName)
    {
        var url = string.Format("/ps2/weaponinfo/byname/{0}", weaponName);
        var result = await _httpClient.GetAsync(url);

        if (!result.IsSuccessStatusCode)
        {
            throw new VoidwellException(result.ReasonPhrase);
        }

        return await GetContentAsync<WeaponInfoResult>(result);
    }

    private async Task<T> GetContentAsync<T>(HttpResponseMessage response) where T : class
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _serializerOptions)!;
    }
}
