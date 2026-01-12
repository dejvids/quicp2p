using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.AppCommands;

namespace QuicPeer.Tests.AppCommands;

public static class AppCommandsMock
{
    private static readonly ConnectCommand _connecCommand;
    private static readonly ShowDataCommand _showDataCommand;

    public static ConnectCommand ConnectCommand { get; }
    public static ShowDataCommand ShowDataCommand { get; }

    static AppCommandsMock()
    {
        ConnectCommand = Substitute.For<ConnectCommandMock>();
        ShowDataCommand = Substitute.For<ShowDataCommandMock>();

        _connecCommand = new ConnectCommandMock();
        _showDataCommand = new ShowDataCommandMock();

        ConnectCommand.CommandName.Returns(_connecCommand.CommandName);
        ShowDataCommand.CommandName.Returns(_showDataCommand.CommandName);
    }

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