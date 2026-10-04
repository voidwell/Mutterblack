using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Mutterblack.Client;
using Mutterblack.Client.Models;

namespace Mutterblack.App.Modules;

[Group("ps2", "Planetside commands")]
[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
public class PlanetsideModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly VoidwellClient _voidwellClient;
    private readonly ILogger<PlanetsideModule> _logger;

    public PlanetsideModule(VoidwellClient voidwellClient, ILogger<PlanetsideModule> logger)
    {
        _voidwellClient = voidwellClient;
        _logger = logger;
    }

    [SlashCommand("player", "Get player stats.")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task GetPlayerStatsAsync(string characterName, string? weaponName = null, PlatformType platformType = PlatformType.PC)
    {
        await DeferAsync();

        try
        {
            if (string.IsNullOrEmpty(weaponName))
            {
                await GetPlayerStatsAsync(platformType, characterName);
            }
            else
            {
                await GetPlayerWeaponStatsAsync(platformType, characterName, weaponName);
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [SlashCommand("outfit", "Get outfit stats.")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task GetOutfitStatsAsync(string outfitAlias, PlatformType platformType = PlatformType.PC)
    {
        await DeferAsync();

        try
        {
            var outfit = await _voidwellClient.GetPlanetsideOutfitStatsAsync(platformType, outfitAlias);

            var fieldBuilders = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder()
                    .WithName("Server")
                    .WithIsInline(false)
                    .WithValue(outfit.WorldName),
                new EmbedFieldBuilder()
                    .WithName("Leader")
                    .WithIsInline(false)
                    .WithValue(outfit.LeaderName),
                new EmbedFieldBuilder()
                    .WithName("Member Count")
                    .WithIsInline(true)
                    .WithValue(outfit.MemberCount),
                new EmbedFieldBuilder()
                    .WithName("Activity 7 Days")
                    .WithIsInline(true)
                    .WithValue(outfit.Activity7Days),
                new EmbedFieldBuilder()
                    .WithName("Activity 30 Days")
                    .WithIsInline(true)
                    .WithValue(outfit.Activity30Days),
                new EmbedFieldBuilder()
                    .WithName("Activity 90 Days")
                    .WithIsInline(true)
                    .WithValue(outfit.Activity90Days)
            };

            var outfitName = outfit.Name;
            if (!string.IsNullOrEmpty(outfit.Alias))
            {
                outfitName = $"[{outfit.Alias}] " + outfitName;
            }

            var embed = new EmbedBuilder()
                .WithAuthor(outfitName)
                .WithTitle("Click here for full stats")
                .WithUrl(string.Format("{0}/ps2/outfit/{1}", Constants.VoidwellUrl, outfit.OutfitId))
                .WithColor(Constants.DefaultEmbedColor)
                .WithThumbnailUrl(CreateCensusImageURI(outfit.FactionImageId))
                .WithFields(fieldBuilders)
                .Build();

            await ModifyOriginalResponseAsync(properties => properties.Embed = embed);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    [SlashCommand("weapon", "Get weapon stats.")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task GetWeaponStatsAsync(string weaponName)
    {
        await DeferAsync();

        try
        {
            var weapon = await _voidwellClient.GetPlanetsideWeaponStatsAsync(weaponName);

            var fieldBuilders = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder()
                    .WithName("Type")
                    .WithIsInline(true)
                    .WithValue(weapon.Category),
                new EmbedFieldBuilder()
                    .WithName("Faction restriction")
                    .WithIsInline(true)
                    .WithValue(weapon.FactionName ?? "None"),
                new EmbedFieldBuilder()
                    .WithName("Range")
                    .WithIsInline(true)
                    .WithValue(weapon.Range)
            };

            if (weapon.FireRateMs > 0)
            {
                fieldBuilders.Add(
                    new EmbedFieldBuilder()
                        .WithName("Fire rate")
                        .WithIsInline(true)
                        .WithValue(string.Format("{0:N0} RPM ({1:N2} s)", 60000.0 / weapon.FireRateMs, weapon.FireRateMs / 1000.0)));
            }

            if (weapon.DamageRadius > 0)
            {
                fieldBuilders.Add(
                    new EmbedFieldBuilder()
                        .WithName("Damage radius")
                        .WithIsInline(true)
                        .WithValue(weapon.DamageRadius));
            }

            fieldBuilders.AddRange(new[]
            {
                new EmbedFieldBuilder()
                    .WithName("Muzzle velocity")
                    .WithIsInline(true)
                    .WithValue(string.Format("{0:N0} m/sec", weapon.MuzzleVelocity)),
                new EmbedFieldBuilder()
                    .WithName("Reload speed")
                    .WithIsInline(true)
                    .WithValue(string.Format("{0:N3} sec / {1:N3} sec", weapon.MinReloadSpeed / 1000.0, weapon.MaxReloadSpeed / 1000.0)),
                new EmbedFieldBuilder()
                    .WithName("Ammunition")
                    .WithIsInline(true)
                    .WithValue(string.Format("{0} / {1}", weapon.ClipSize, weapon.Capacity)),
            });

            if (weapon.IronSightZoom > 0)
            {
                fieldBuilders.Add(
                    new EmbedFieldBuilder()
                        .WithName("Iron sight zoom")
                        .WithIsInline(true)
                        .WithValue(string.Format("{0:N2}", weapon.IronSightZoom)));
            }

            fieldBuilders.Add(
                new EmbedFieldBuilder()
                    .WithName("Fire modes")
                    .WithIsInline(false)
                    .WithValue(string.Join(" / ", weapon.FireModes)));

            if (!weapon.IsVehicleWeapon)
            {
                fieldBuilders.Add(
                    new EmbedFieldBuilder()
                        .WithName("Damage")
                        .WithIsInline(false)
                        .WithValue(string.Format("{0} / {1}m / {2} / {3}m", weapon.MaxDamage, weapon.MaxDamageRange, weapon.MinDamage, weapon.MinDamageRange)));
            }

            if (!weapon.IsVehicleWeapon && weapon.IndirectMaxDamage > 0)
            {
                fieldBuilders.Add(
                    new EmbedFieldBuilder()
                        .WithName("Indirect damage")
                        .WithIsInline(false)
                        .WithValue(string.Format("{0} / {1}m / {2} / {3}m", weapon.IndirectMaxDamage, weapon.IndirectMaxDamageRange, weapon.IndirectMinDamage, weapon.IndirectMinDamageRange)));
            }

            if (!weapon.IsVehicleWeapon && weapon.HipAcc != null)
            {
                fieldBuilders.Add(
                    new EmbedFieldBuilder()
                        .WithName("Hip accuracy")
                        .WithIsInline(false)
                        .WithValue(string.Format("{0:N2} / {1:N2} / {2:N2} / {3:N2} / {4:N2}", weapon.HipAcc.Crouching, weapon.HipAcc.CrouchWalking, weapon.HipAcc.Standing, weapon.HipAcc.Running, weapon.HipAcc.Cof)));
            }

            if (!weapon.IsVehicleWeapon && weapon.AimAcc != null)
            {
                fieldBuilders.Add(
                    new EmbedFieldBuilder()
                        .WithName("Aim accuracy")
                        .WithIsInline(false)
                        .WithValue(string.Format("{0:N2} / {1:N2} / {2:N2} / {3:N2} / {4:N2}", weapon.AimAcc.Crouching, weapon.AimAcc.CrouchWalking, weapon.AimAcc.Standing, weapon.AimAcc.Running, weapon.AimAcc.Cof)));
            }

            var embed = new EmbedBuilder()
                .WithAuthor(weapon.Name)
                .WithTitle("Click here for full stats")
                .WithUrl(string.Format("{0}/ps2/item/{1}", Constants.VoidwellUrl, weapon.ItemId))
                .WithColor(Constants.DefaultEmbedColor)
                .WithThumbnailUrl(CreateCensusImageURI(weapon.ImageId))
                .WithDescription(weapon.Description)
                .WithFields(fieldBuilders)
                .Build();

            await ModifyOriginalResponseAsync(properties => properties.Embed = embed);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(ex);
        }
    }

    private async Task GetPlayerStatsAsync(PlatformType platformType, string characterName)
    {
        var stats = await _voidwellClient.GetPlanetsideCharacterStatsAsync(platformType, characterName);

        var fieldBuilders = new List<EmbedFieldBuilder>
        {
            new EmbedFieldBuilder()
                .WithName("Last Seen")
                .WithIsInline(false)
                .WithValue(string.Format("{0:yyyy-MM-dd hh:mm:ss} UTC", stats.LastSaved)),
            new EmbedFieldBuilder()
                .WithName("Server")
                .WithIsInline(true)
                .WithValue(stats.World)
        };

        if (!string.IsNullOrEmpty(stats.OutfitName))
        {
            var outfitValue = stats.OutfitName;
            if (!string.IsNullOrEmpty(stats.OutfitAlias))
            {
                outfitValue = $"[{stats.OutfitAlias}] {outfitValue}";
            }

            fieldBuilders.Add(
                new EmbedFieldBuilder()
                    .WithName("Outfit")
                    .WithIsInline(true)
                    .WithValue(outfitValue));
        }

        fieldBuilders.AddRange(new[]
        {
            new EmbedFieldBuilder()
                .WithName("Battle Rank")
                .WithIsInline(false)
                .WithValue(stats.BattleRank),
            new EmbedFieldBuilder()
                .WithName("Kills")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N0}", stats.Kills)),
            new EmbedFieldBuilder()
                .WithName("Play Time")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N1} ({1:N1}) Hours", stats.PlayTime / 3600.0, stats.TotalPlayTimeMinutes / 60.0)),
            new EmbedFieldBuilder()
                .WithName("KDR")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N2}", stats.KillDeathRatio)),
            new EmbedFieldBuilder()
                .WithName("HSR")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N2}%", stats.HeadshotRatio * 100)),
            new EmbedFieldBuilder()
                .WithName("KpH")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N2} ({1:N2})", stats.KillsPerHour, stats.TotalKillsPerHour)),
            new EmbedFieldBuilder()
                .WithName("Siege Level")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N1}", stats.SiegeLevel)),
            new EmbedFieldBuilder()
                .WithName("IVI Score")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N0}", stats.IVIScore)),
            new EmbedFieldBuilder()
                .WithName("IVI KDR")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N2}", stats.IVIKillDeathRatio))
        });

        var embed = new EmbedBuilder()
            .WithAuthor(stats.Name)
            .WithTitle("Click here for full stats")
            .WithUrl(string.Format("{0}/ps2/player/{1}", Constants.VoidwellUrl, stats.Id))
            .WithColor(Constants.DefaultEmbedColor)
            .WithThumbnailUrl(CreateCensusImageURI(stats.FactionImageId))
            .WithFields(fieldBuilders)
            .Build();

        await ModifyOriginalResponseAsync(properties => properties.Embed = embed);
    }

    private async Task GetPlayerWeaponStatsAsync(PlatformType platformType, string characterName, string weaponName)
    {
        var stats = await _voidwellClient.GetPlanetsideCharacterWeaponStatsAsync(platformType, characterName, weaponName);

        var fieldBuilders = new List<EmbedFieldBuilder>
        {
            new EmbedFieldBuilder()
                .WithName("Kills")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N0}", stats.Kills)),
            new EmbedFieldBuilder()
                .WithName("Deaths")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N0}", stats.Deaths)),
            new EmbedFieldBuilder()
                .WithName("Play Time")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N0} Minutes", stats.PlayTime / 60.0)),
            new EmbedFieldBuilder()
                .WithName("Score")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N0}", stats.Score)),
            new EmbedFieldBuilder()
                .WithName("KpH")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N2}", stats.KillsPerHour)),
            new EmbedFieldBuilder()
                .WithName("KpH Δ")
                .WithIsInline(true)
                .WithValue(stats.KillsPerHourGrade),
            new EmbedFieldBuilder()
                .WithName("KDR")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N2}", stats.KillDeathRatio)),
            new EmbedFieldBuilder()
                .WithName("KDR Δ")
                .WithIsInline(true)
                .WithValue(stats.KillDeathRatioGrade),
            new EmbedFieldBuilder()
                .WithName("HSR")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N2}%", stats.HeadshotRatio * 100)),
            new EmbedFieldBuilder()
                .WithName("HSR Δ")
                .WithIsInline(true)
                .WithValue(stats.HeadshotRatioGrade),
            new EmbedFieldBuilder()
                .WithName("Accuracy")
                .WithIsInline(true)
                .WithValue(string.Format("{0:N2}%", stats.Accuracy * 100)),
            new EmbedFieldBuilder()
                .WithName("Accuracy Δ")
                .WithIsInline(true)
                .WithValue(stats.AccuracyGrade)
        };

        var embed = new EmbedBuilder()
            .WithAuthor(string.Format("{0} [{1}]", stats.CharacterName, stats.WeaponName))
            .WithTitle("Click here for full stats")
            .WithUrl(string.Format("{0}/ps2/player/{1}", Constants.VoidwellUrl, stats.CharacterId))
            .WithColor(Constants.DefaultEmbedColor)
            .WithThumbnailUrl(CreateCensusImageURI(stats.WeaponImageId))
            .WithFields(fieldBuilders)
            .Build();

        await ModifyOriginalResponseAsync(properties => properties.Embed = embed);
    }

    private static string CreateCensusImageURI(int? imageId)
    {
        return string.Format("{0}/{1}.png", Constants.CensusImageBaseUrl, imageId);
    }

    private async Task HandleExceptionAsync(Exception ex)
    {
        var clientMessage = "Something went wrong!";

        _logger.LogError(ex, "Interaction error");

        if (ex is BotException bex)
        {
            clientMessage = bex.ClientMessage;
        }
        else if (ex is VoidwellException)
        {
            clientMessage = ex.Message;
        }

        await Context.Interaction.ModifyOriginalResponseAsync(properties =>
                    properties.Content = clientMessage);
    }
}
