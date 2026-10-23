using Microsoft.Win32;

namespace ScrollFlip;

internal sealed class TrayAppContext : ApplicationContext
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunValueName = "ScrollFlip";

    private readonly Settings _settings = Settings.Load();
    private readonly ScrollInverter _inverter = new();
    private readonly HotkeyWindow _hotkeyWindow = new();
    private readonly NotifyIcon _trayIcon;
    private readonly ToolStripMenuItem _invertedItem;
    private readonly ToolStripMenuItem _horizontalItem;
    private readonly ToolStripMenuItem _startupItem;
    private readonly Icon _iconNormal = IconFactory.Create(false);
    private readonly Icon _iconInverted = IconFactory.Create(true);

    public TrayAppContext()
    {
        _inverter.Enabled = _settings.Inverted;
        _inverter.InvertHorizontal = _settings.InvertHorizontal;
        _inverter.Start();

        _invertedItem = new ToolStripMenuItem("Scroll invertido\tCtrl+Alt+S", null, (_, _) => Toggle())
        {
            Checked = _settings.Inverted,
            CheckOnClick = false
        };

        _horizontalItem = new ToolStripMenuItem("Inverter também o scroll horizontal", null, (_, _) =>
        {
            _settings.InvertHorizontal = !_settings.InvertHorizontal;
            _inverter.InvertHorizontal = _settings.InvertHorizontal;
            _horizontalItem!.Checked = _settings.InvertHorizontal;
            _settings.Save();
        })
        {
            Checked = _settings.InvertHorizontal
        };

        _startupItem = new ToolStripMenuItem("Iniciar com o Windows", null, (_, _) => ToggleStartup())
        {
            Checked = IsStartupEnabled()
        };

        var menu = new ContextMenuStrip();
        menu.Items.Add(_invertedItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_horizontalItem);
        menu.Items.Add(_startupItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("Sair", null, (_, _) => ExitApp()));

        _trayIcon = new NotifyIcon
        {
            Icon = _settings.Inverted ? _iconInverted : _iconNormal,
            ContextMenuStrip = menu,
            Visible = true,
            Text = BuildTooltip()
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                Toggle();
            }
        };

        _hotkeyWindow.HotkeyPressed += (_, _) => Toggle();
        if (!_hotkeyWindow.Register())
        {
            _trayIcon.ShowBalloonTip(
                5000,
                "ScrollFlip",
                "Não foi possível registrar Ctrl+Alt+S (atalho já em uso). Use o ícone na bandeja para alternar.",
                ToolTipIcon.Warning);
        }
    }

    private void Toggle()
    {
        _settings.Inverted = !_settings.Inverted;
        _inverter.Enabled = _settings.Inverted;
        _invertedItem.Checked = _settings.Inverted;
        _trayIcon.Icon = _settings.Inverted ? _iconInverted : _iconNormal;
        _trayIcon.Text = BuildTooltip();
        _settings.Save();

        _trayIcon.ShowBalloonTip(
            1200,
            "ScrollFlip",
            _settings.Inverted ? "Scroll invertido" : "Scroll normal",
            ToolTipIcon.None);
    }

    private string BuildTooltip() =>
        _settings.Inverted
            ? "ScrollFlip — scroll invertido (Ctrl+Alt+S)"
            : "ScrollFlip — scroll normal (Ctrl+Alt+S)";

    private static bool IsStartupEnabled()
    {
        using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(RunValueName) is not null;
    }

    private void ToggleStartup()
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (IsStartupEnabled())
            {
                key.DeleteValue(RunValueName, throwOnMissingValue: false);
                _startupItem.Checked = false;
            }
            else
            {
                key.SetValue(RunValueName, $"\"{Environment.ProcessPath}\"");
                _startupItem.Checked = true;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Não foi possível alterar a inicialização automática:\n{ex.Message}",
                "ScrollFlip",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void ExitApp()
    {
        _trayIcon.Visible = false;
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            _hotkeyWindow.Dispose();
            _inverter.Dispose();
            _iconNormal.Dispose();
            _iconInverted.Dispose();
        }

        base.Dispose(disposing);
    }
}
