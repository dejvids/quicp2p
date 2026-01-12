using QuicPeer.AppCommands;

namespace QuicPeer.Tests.AppCommands;

public static class AppCommandsMock
{
    public static ConnectCommand ConnectCommand { get; } = new ConnectCommandMock();
    public static ShowDataCommand ShowDataCommand { get; } = new ShowDataCommandMock();


    public class ConnectCommandMock : ConnectCommand
    {
        public ConnectCommandMock()
            : base(null, null, null, null, null)
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
            : base(null, null)
        {
        }

        public override ValueTask Execute(CancellationToken cancellationToken)
        {
            return ValueTask.CompletedTask;
        }
    }

}