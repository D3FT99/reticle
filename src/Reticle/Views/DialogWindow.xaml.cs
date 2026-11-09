using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Reticle.Localization;

namespace Reticle.Views;

/// <summary>Modal message with one or two large buttons.</summary>
public partial class DialogWindow : Window
{
    private DialogWindow(string iconKey, string heading, string message, string primary, string? secondary, bool dangerPrimary)
    {
        InitializeComponent();
        IconPath.Data = (Geometry)FindResource(iconKey);
        HeadingText.Text = heading;
        MessageText.Text = message;
        MessageText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        PrimaryButton.Content = primary;
        if (dangerPrimary)
        {
            PrimaryButton.Style = (Style)FindResource("DangerButton");
            IconBadge.Background = (Brush)FindResource("DangerSoftBrush");
            IconPath.Stroke = (Brush)FindResource("DangerBrush");
        }
        if (secondary is null)
        {
            SecondaryButton.Visibility = Visibility.Collapsed;
            Grid.SetColumn(PrimaryButton, 0);
            Grid.SetColumnSpan(PrimaryButton, 3);
        }
        else
        {
            SecondaryButton.Content = secondary;
        }
    }

    /// <summary>"Are you sure that you want to exit?" — true for Yes.</summary>
    public static bool ConfirmExit(Window? owner) =>
        ShowModal(new DialogWindow("IconPower", Localizer.Get("ExitDialog_Heading"), Localizer.Get("ExitDialog_Message"),
            Localizer.Get("ExitDialog_Yes"), Localizer.Get("ExitDialog_No"), dangerPrimary: true), owner);

    public static void Notify(Window? owner, string heading, string message) =>
        ShowModal(new DialogWindow("IconShield", heading, message, Localizer.Get("Common_OK"), null, dangerPrimary: false), owner);

    private static bool ShowModal(DialogWindow dialog, Window? owner)
    {
        if (owner is { IsVisible: true })
        {
            dialog.Owner = owner;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        return dialog.ShowDialog() == true;
    }

    private void OnPrimary(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnSecondary(object sender, RoutedEventArgs e) => DialogResult = false;
}
