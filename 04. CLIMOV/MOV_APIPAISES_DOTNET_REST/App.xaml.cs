using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;

namespace MOV_APIPAISES_DOTNET_REST
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        public App()
        {
            InitializeComponent();
            UserAppTheme = AppTheme.Light;
            // Con el teclado abierto, la pantalla se reduce para que el formulario siga visible (adjustResize).
            On<Microsoft.Maui.Controls.PlatformConfiguration.Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}
