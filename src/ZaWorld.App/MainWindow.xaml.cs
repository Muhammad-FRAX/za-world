using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ZaWorld.App.Media;
using ZaWorld.App.Services;
using ZaWorld.Core.Configuration;
using ZaWorld.Core.Hotkeys;

namespace ZaWorld.App;

public partial class MainWindow : Window
{
    private readonly AppSettingsStore _settingsStore = new();
    private InputLockService? _inputLock;
    private WebcamSnapshotService? _webcam;
    private string _activeCaptureFolder = string.Empty;
    private bool _isLocked;

    public MainWindow()
    {
        InitializeComponent();
        ApplyBranding();
        LoadSettings();
        UpdateLockUi();
    }

    protected override void OnClosed(EventArgs e)
    {
        _inputLock?.Dispose();
        _webcam?.Dispose();
        base.OnClosed(e);
    }

    private void ApplyBranding()
    {
        var icoPath = Path.Combine(AppContext.BaseDirectory, "theworld-logo.ico");
        if (File.Exists(icoPath))
        {
            Icon = LoadBitmapFromFile(icoPath);
            LogoImage.Source = LoadBitmapFromFile(icoPath);
            return;
        }

        var webpPath = Path.Combine(AppContext.BaseDirectory, "theworld-logo.webp");
        var webp = WebpImageLoader.TryLoadBitmapImage(webpPath);
        if (webp is null)
        {
            return;
        }

        Icon = webp;
        LogoImage.Source = WebpImageLoader.TryLoadBitmapImage(webpPath);
    }

    private static BitmapImage LoadBitmapFromFile(string path)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(Path.GetFullPath(path), UriKind.Absolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void LoadSettings()
    {
        var settings = _settingsStore.LoadOrCreateDefault();
        UnlockHotkeyTextBox.Text = settings.UnlockHotkey;
        CaptureFolderTextBox.Text = settings.CaptureFolderPath;
        StatusText.Text = "Settings loaded.";
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        SaveSettingsInternal();
        StatusText.Text = "Settings saved.";
    }

    private void Lock_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SaveSettingsInternal();
            var settings = _settingsStore.LoadOrCreateDefault();
            var chord = UnlockHotkeyParser.Parse(settings.UnlockHotkey);
            _activeCaptureFolder = GetEffectiveCaptureFolder(settings.CaptureFolderPath);

            _inputLock ??= new InputLockService();
            _webcam ??= new WebcamSnapshotService();

            _inputLock.UnlockRequested -= OnUnlockRequested;
            _inputLock.IntrusionDetected -= OnIntrusionDetected;
            _inputLock.UnlockRequested += OnUnlockRequested;
            _inputLock.IntrusionDetected += OnIntrusionDetected;

            _inputLock.Start(chord);
            _isLocked = true;
            UpdateLockUi();
            StatusText.Text = "Locked. Use your hotkey to unlock. Window minimized — restore from taskbar if needed.";
            WindowState = WindowState.Minimized;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Za-World", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Unlock_Click(object sender, RoutedEventArgs e)
    {
        UnlockFromUi();
    }

    private void OnUnlockRequested()
    {
        // Must not Unhook from inside the hook callback — queue after the hook returns.
        Dispatcher.BeginInvoke(UnlockFromUi, DispatcherPriority.Background);
    }

    private void OnIntrusionDetected()
    {
        if (string.IsNullOrWhiteSpace(_activeCaptureFolder))
        {
            return;
        }

        _webcam ??= new WebcamSnapshotService();
        _webcam.TryCaptureJpeg(_activeCaptureFolder);
    }

    private void UnlockFromUi()
    {
        _inputLock?.Stop();
        _isLocked = false;
        UpdateLockUi();
        StatusText.Text = "Unlocked.";
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void SaveSettingsInternal()
    {
        var settings = new AppSettings
        {
            UnlockHotkey = UnlockHotkeyTextBox.Text.Trim(),
            CaptureFolderPath = CaptureFolderTextBox.Text.Trim(),
        };

        _settingsStore.Save(settings);
    }

    private void UpdateLockUi()
    {
        UnlockHotkeyTextBox.IsEnabled = !_isLocked;
        CaptureFolderTextBox.IsEnabled = !_isLocked;
    }

    private static string GetEffectiveCaptureFolder(string? customPath)
    {
        var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        var resolver = new CapturePathResolver();
        return resolver.GetEffectiveCaptureFolder(
            customPath,
            pictures,
            Directory.Exists,
            IsFolderWritable);
    }

    private static bool IsFolderWritable(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            var probe = Path.Combine(path, $".za-world-write-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
