namespace Mutterblack.Client.Models;

public class WeaponInfoResult
{
    public string Name { get; set; } = null!;
    public int ItemId { get; set; }
    public string Category { get; set; } = null!;
    public int? FactionId { get; set; }
    public string FactionName { get; set; } = null!;
    public int? ImageId { get; set; }
    public string Description { get; set; } = null!;
    public int MaxStackSize { get; set; }
    public string Range { get; set; } = null!;
    public int? FireRateMs { get; set; }
    public int? ClipSize { get; set; }
    public int? Capacity { get; set; }
    public int? MuzzleVelocity { get; set; }
    public int? MinDamage { get; set; }
    public int? MaxDamage { get; set; }
    public int? MinDamageRange { get; set; }
    public int? MaxDamageRange { get; set; }
    public int? IndirectMaxDamage { get; set; }
    public int? IndirectMinDamage { get; set; }
    public float? IndirectMaxDamageRange { get; set; }
    public float? IndirectMinDamageRange { get; set; }
    public int? MinReloadSpeed { get; set; }
    public int? MaxReloadSpeed { get; set; }
    public float? IronSightZoom { get; set; }
    public IEnumerable<string> FireModes { get; set; } = null!;
    public AccuracyState HipAcc { get; set; } = null!;
    public AccuracyState AimAcc { get; set; } = null!;
    public bool IsVehicleWeapon { get; set; }
    public int? DamageRadius { get; set; }
}

public class AccuracyState
{
    public float? Crouching { get; set; }
    public float? CrouchWalking { get; set; }
    public float? Standing { get; set; }
    public float? Running { get; set; }
    public float? Cof { get; set; }
}
