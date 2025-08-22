using ClientApp.Views;

namespace ClientApp;

    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

        // Register the route using the type from the Views namespace
        Routing.RegisterRoute(nameof(CameraDetectionPage), typeof(CameraDetectionPage));
        // Register the route for the Map page
        Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
    }
}
