namespace Mutterblack.Client.Authentication;

public interface IVoidwellTokenService
{
    Task<string> GetTokenAsync(CancellationToken cancellationToken = default);

    void Invalidate(string rejectedToken);
}
