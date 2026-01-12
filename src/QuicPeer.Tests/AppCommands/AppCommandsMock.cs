using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.AppCommands;

namespace QuicPeer.Tests.AppCommands;

public static class AppCommandsMock
{
    public static ConnectCommand ConnectCommand { get; } = new ConnectCommandMock();
    public static ShowDataCommand ShowDataCommand { get; } = new ShowDataCommandMock();

    public class ConnectCommandMock : ConnectCommand
    {
        public ConnectCommandMock()
            : base(Substitute.For<ILogger<ConnectCommand>>(), Substitute.For<IConsoleAccessor>(), null, new SendCommandMock(), new SendFileCommandMock())
        {
        }

        public override ValueTask Execute(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

    public class ShowDataCommandMock : ShowDataCommand
    {
        public ShowDataCommandMock()
            : base(Substitute.For<ILogger<ShowDataCommand>>(), Substitute.For<IConsoleAccessor>())
        {
        }

        public override ValueTask Execute(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
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
            : base(Substitute.For<ILogger<SendFileCommand>>(), Substitute.For<IConsoleAccessor>())
        {
        }
    }

}