namespace Mutterblack.App;

public class BotException : Exception
{
    public string? ClientMessage { get; private set; }

    public BotException(string? clientMessage)
    {
        ClientMessage = clientMessage;
    }

    public BotException(string? clientMessage, string errorMessage)
        : base(errorMessage)
    {
        ClientMessage = clientMessage;
    }
}
