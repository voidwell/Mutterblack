namespace Mutterblack.Client.Models;

public class OutfitDetails
{
    public string OutfitId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Alias { get; set; } = null!;
    public int? FactionId { get; set; }
    public string FactionName { get; set; } = null!;
    public int? FactionImageId { get; set; }
    public int? WorldId { get; set; }
    public string WorldName { get; set; } = null!;
    public DateTime CreatedDate { get; set; }
    public string LeaderCharacterId { get; set; } = null!;
    public string LeaderName { get; set; } = null!;
    public int MemberCount { get; set; }
    public int TrackedMemberCount { get; set; }
    public int Activity7Days { get; set; }
    public int Activity30Days { get; set; }
    public int Activity90Days { get; set; }
}
