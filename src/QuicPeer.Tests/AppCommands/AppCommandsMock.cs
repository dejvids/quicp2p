using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.AppCommands;

namespace QuicPeer.Tests.AppCommands;

public static class AppCommandsMock
{
    public static ConnectCommand ConnectCommand { get; }
    public static ShowDataCommand ShowDataCommand { get; }

    static AppCommandsMock()
    {
        var connectCommand = new ConnectCommandMock();
        var showDataCommand = new ShowDataCommandMock();

        ConnectCommand = Substitute.For<ConnectCommandMock>();
        ShowDataCommand = Substitute.For<ShowDataCommandMock>();
        
        ConnectCommand.CommandName.Returns(connectCommand.CommandName);
        ShowDataCommand.CommandName.Returns(showDataCommand.CommandName);
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
                new SendCommandMock(), 
                new SendFileCommandMock())
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