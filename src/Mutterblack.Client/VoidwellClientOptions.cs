using System.ComponentModel.DataAnnotations;

namespace Mutterblack.Client;

public class VoidwellClientOptions
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Voidwell:TokenServiceAddress is required")]
    public string TokenServiceAddress { get; set; } = null!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Voidwell:ClientId is required")]
    public string ClientId { get; set; } = null!;

    [Required(AllowEmptyStrings = false, ErrorMessage = "Voidwell:ClientSecret is required")]
    public string ClientSecret { get; set; } = null!;

    [Required(ErrorMessage = "Voidwell:ClientScopes is required")]
    [MinLength(1, ErrorMessage = "Voidwell:ClientScopes must contain at least one scope")]
    public List<string> ClientScopes { get; set; } = ["voidwell-daybreakgames"];
}
