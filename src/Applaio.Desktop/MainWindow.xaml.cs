using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace Applaio.Desktop;

public sealed partial class MainWindow : Window
{
    private DesktopAcrylicController? _acrylicController;
    private SystemBackdropConfiguration? _backdropConfiguration;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
        TrySetBackdrop();
    }

    private void ConfigureWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Resize(new Windows.Graphics.SizeInt32(1280, 820));
        appWindow.Title = "Applaio";
    }

    private void TrySetBackdrop()
    {
        if (!DesktopAcrylicController.IsSupported())
        {
            return;
        }

        _backdropConfiguration = new SystemBackdropConfiguration
        {
            IsInputActive = true,
            Theme = SystemBackdropTheme.Default
        };

        _acrylicController = new DesktopAcrylicController();
        _acrylicController.SetSystemBackdropConfiguration(_backdropConfiguration);
        _acrylicController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
    }

    private void RootNavigation_SelectionChanged(
        NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            PageTitle.Text = "Ustawienia";
            PageSubtitle.Text = "Konfiguracja Applaio.";
            return;
        }

        if (args.SelectedItemContainer is not NavigationViewItem item)
        {
            return;
        }

        (PageTitle.Text, PageSubtitle.Text) = item.Tag switch
        {
            "recruitments" => ("Rekrutacje", "Pipeline, rozmowy, statusy i historia kontaktu."),
            "companies" => ("Firmy", "Firmy, do których aplikujesz lub z którymi rozmawiasz."),
            "import" => ("Import", "Migracja danych z Excela i przyszłe integracje."),
            _ => ("Dashboard", "Śledź cały proces rekrutacji w jednym miejscu.")
        };
    }
}
