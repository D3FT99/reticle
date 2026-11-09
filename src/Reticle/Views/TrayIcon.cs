using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Reticle.Localization;

namespace Reticle.Views;

/// <summary>Notification-area icon: the app's only presence outside the toolbar. Left click opens Settings.</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ToolStripMenuItem _settingsItem;
    private readonly ToolStripMenuItem _closeItem;

    public TrayIcon(Action showSettings, Action closeApplication)
    {
        _settingsItem = new ToolStripMenuItem(string.Empty, null, (_, _) => showSettings());
        _closeItem = new ToolStripMenuItem(string.Empty, null, (_, _) => closeApplication());
        var menu = new ContextMenuStrip();
        menu.Items.AddRange([_settingsItem, new ToolStripSeparator(), _closeItem]);

        _icon = new NotifyIcon { Icon = LoadIcon(), ContextMenuStrip = menu };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                showSettings();
            }
        };
        UpdateTexts();
        Localizer.Changed += OnLanguageChanged;
        _icon.Visible = true;
    }

    public void Dispose()
    {
        Localizer.Changed -= OnLanguageChanged;
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Icon?.Dispose();
        _icon.Dispose();
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => UpdateTexts();

    private void UpdateTexts()
    {
        _icon.Text = Localizer.Get("AppName");
        _settingsItem.Text = Localizer.Get("Tray_Settings");
        _closeItem.Text = Localizer.Get("Tray_Close");
    }

    private static Icon LoadIcon()
    {
        using Stream stream = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Assets/Reticle.ico")).Stream;
        return new Icon(stream, SystemInformation.SmallIconSize);
    }
}
