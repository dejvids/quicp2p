using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using QuicPeer.AppCommands;
using QuicPeer.Client.Abstraction;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ClientQueue;
using QuicPeer.Common.Messaging.ServerQueue;
using QuicPeer.Options;

namespace QuicPeer.Tests.AppCommands;

public static class AppCommandsMock
{
    public static ConnectCommand ConnectCommand => 
        MockCommand<ConnectCommand, ConnectCommandMock>();

    public static ShowDataCommand ShowDataCommand => 
        MockCommand<ShowDataCommand, ShowDataCommandMock>();

    public static UnlockCommand UnlockCommand => 
        MockCommand<UnlockCommand, UnlockCommandMock>();

    public class ConnectCommandMock : ConnectCommand
    {
        public static SendCommand SendCommand => MockCommand<SendCommand, SendCommandMock>();
        public static SendFileCommand SendFileCommand => MockCommand<SendFileCommand, SendFileCommandMock>();

        public ConnectCommandMock()
            : base(Substitute.For<ILogger<ConnectCommand>>(), 
                Substitute.For<IConsoleAccessor>(), 
                null!, 
                [new SendCommandMock(), new SendFileCommandMock()])
        {
        }

        public override ValueTask<CommandResult> Execute(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(CommandResult.Success);
        }
    }

    public class ShowDataCommandMock : ShowDataCommand
    {
        public ShowDataCommandMock()
            : base(Substitute.For<ILogger<ShowDataCommand>>(),
                Substitute.For<IConsoleAccessor>(),
                Substitute.For<IMessageQueue<IServerMessage>>())
        {
        }

        public override ValueTask<CommandResult> Execute(CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(CommandResult.Success);
        }
    }

    public class SendCommandMock : SendCommand
    {
        public SendCommandMock() 
            : base(Substitute.For<ILogger<SendCommand>>(), Substitute.For<IConsoleAccessor>())
        {
        }
    }

    public class SendFileCommandMock : SendFileCommand
    {
        public SendFileCommandMock()
            : base(Substitute.For<ILogger<SendFileCommand>>(), Substitute.For<IConsoleAccessor>(),
                Substitute.For<IFileSystem>())
        {
        }
    }

    public class UnlockCommandMock : UnlockCommand
    {
        public UnlockCommandMock() 
            : base(Substitute.For<ILogger<UnlockCommand>>(), 
                Substitute.For<IConsoleAccessor>(), 
                Substitute.For<IMessageQueue<IClientMessage>>(),
                Substitute.For<IOptions<CertificateOptions>>(),
                Substitute.For<IPeerClientFactory>(),
                Substitute.For<IFileSystem>())
        {
        }
    }
    private static TCommand MockCommand<TCommand, TMock>()
       where TMock : AppCommand, TCommand, new()
    {
        var command = new TMock();
        var mock = Substitute.For<TMock>();
        mock.Execute(Arg.Any<CancellationToken>())
            .Returns(CommandResult.Success);

        mock.CommandName.Returns(command.CommandName);
        mock.CommandName.Returns(mock.CommandName);

        return mock;
    }
}