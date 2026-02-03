namespace QuicPeer.AppCommands;

public class CommandResult
{
    public bool IsSuccess { get; private set; }
    public bool Exit { get; private set; }

    private CommandResult()
    {
        IsSuccess = true;
    }

    public static CommandResult Success { get; } = new();
    public static CommandResult Fail { get; } = new() { IsSuccess = false };
    public static CommandResult Done { get; } = new() { Exit = true };
    public static CommandResult Error { get; } = new() { IsSuccess = false, Exit = true };
}