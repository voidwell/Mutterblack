namespace Mutterblack.Client.Models;

public class CharacterWeaponDetails
{
    public string CharacterId { get; set; } = null!;
    public string CharacterName { get; set; } = null!;
    public int ItemId { get; set; }
    public string WeaponName { get; set; } = null!;
    public int? WeaponImageId { get; set; }
    public int? Kills { get; set; }
    public int? Deaths { get; set; }
    public int? PlayTime { get; set; }
    public int? Score { get; set; }
    public int? Headshots { get; set; }
    public double? KillDeathRatio { get; set; }
    public double? HeadshotRatio { get; set; }
    public double? KillsPerHour { get; set; }
    public double? Accuracy { get; set; }
    public string KillDeathRatioGrade { get; set; } = null!;
    public string HeadshotRatioGrade { get; set; } = null!;
    public string KillsPerHourGrade { get; set; } = null!;
    public string AccuracyGrade { get; set; } = null!;
}
