using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using QuicPeer.AppCommands;
using QuicPeer.Client.Abstraction;
using QuicPeer.Common;
using QuicPeer.Options;

namespace QuicPeer.Tests.AppCommands;

public static class AppCommandsMock
{
    public static ConnectCommand ConnectCommand { get; }
    public static ShowDataCommand ShowDataCommand { get; }
    public static UnlockCommand UnlockCommand { get; }

    static AppCommandsMock()
    {
        var connectCommand = new ConnectCommandMock();
        var showDataCommand = new ShowDataCommandMock();
        var unlockCommand = new UnlockCommandMock();

        ConnectCommand = Substitute.For<ConnectCommandMock>();
        ShowDataCommand = Substitute.For<ShowDataCommandMock>();
        UnlockCommand = Substitute.For<UnlockCommandMock>();

        
        ConnectCommand.CommandName.Returns(connectCommand.CommandName);
        ShowDataCommand.CommandName.Returns(showDataCommand.CommandName);
        UnlockCommand.CommandName.Returns(unlockCommand.CommandName);
        UnlockCommand.Execute(Arg.Any<CancellationToken>()).Returns(CommandResult.Success);
    }

    public class ConnectCommandMock : ConnectCommand
    {
        public static SendCommand SendCommand { get; }
        public static SendFileCommand SendFileCommand { get; }

        static ConnectCommandMock()
        {
            var sendCommand = new SendCommandMock();
            var sendFileCommand = new SendFileCommandMock();

            SendCommand = Substitute.For<SendCommandMock>();
            SendFileCommand = Substitute.For<SendFileCommandMock>();

            SendCommand.CommandName.Returns(sendCommand.CommandName);
            SendFileCommand.CommandName.Returns(sendFileCommand.CommandName);
        }

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
            : base(Substitute.For<ILogger<ShowDataCommand>>(), Substitute.For<IConsoleAccessor>())
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
                Substitute.For<IMessageQueue<IConsoleMessage>>(),
                Substitute.For<IOptions<CertificateOptions>>(),
                Substitute.For<IPeerClientFactory>(),
                Substitute.For<IFileSystem>())
        {
        }
    }

}