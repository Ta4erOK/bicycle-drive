using System.Windows;

namespace BicycleDrive
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            ShowLogin();
        }

        /// <summary>Показывает окно входа, затем главное окно списка товаров.</summary>
        private void ShowLogin()
        {
            var login = new LoginWindow();
            bool? result = login.ShowDialog();

            if (result != true)
            {
                Shutdown();
                return;
            }

            var list = new ProductListWindow(login.SessionUser, login.IsGuest);
            list.Closed += (sender, args) =>
            {
                if (list.LogoutRequested)
                {
                    ShowLogin();
                }
                else
                {
                    Shutdown();
                }
            };
            list.Show();
        }
    }
}
