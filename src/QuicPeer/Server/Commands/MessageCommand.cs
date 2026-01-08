namespace QuicPeer.Server.Commands;

public record MessageCommand(string From, string Message, TimeOnly Time) : IServerCommand;
