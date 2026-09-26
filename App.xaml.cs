using System.Windows;

namespace TicketLog.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var desbloqueio = new PinWindow();
        if (desbloqueio.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        MainWindow = new MainWindow();
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow.Show();
    }
}

