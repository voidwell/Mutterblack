namespace Mutterblack.Client.Models;

public class SimpleCharacterDetails
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string World { get; set; } = null!;
    public int FactionId { get; set; }
    public string FactionName { get; set; } = null!;
    public int? FactionImageId { get; set; }
    public int BattleRank { get; set; }
    public string OutfitAlias { get; set; } = null!;
    public string OutfitName { get; set; } = null!;
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int PlayTime { get; set; }
    public int TotalPlayTimeMinutes { get; set; }
    public int Score { get; set; }
    public double KillDeathRatio { get; set; }
    public double HeadshotRatio { get; set; }
    public double KillsPerHour { get; set; }
    public double TotalKillsPerHour { get; set; }
    public double SiegeLevel { get; set; }
    public int IVIScore { get; set; }
    public double IVIKillDeathRatio { get; set; }
    public DateTime? LastSaved { get; set; }
    public int Prestige { get; set; }
    public string MostPlayedWeaponName { get; set; } = null!;
    public int? MostPlayedWeaponId { get; set; }
    public int? MostPlayedWeaponKills { get; set; }
    public string MostPlayedClassName { get; set; } = null!;
    public int? MostPlayedClassId { get; set; }
    public int PlayTimeInMax { get; set; }
}
