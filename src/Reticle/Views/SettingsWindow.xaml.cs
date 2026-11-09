using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Reticle.Core;
using Reticle.Localization;
using Reticle.ViewModels;

namespace Reticle.Views;

public partial class SettingsWindow : Window, ISettingsDialogs
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Dialogs = this;
        viewModel.CloseRequested += OnCloseRequested;
        Closed += (_, _) =>
        {
            viewModel.CloseRequested -= OnCloseRequested;
            viewModel.Dialogs = null;
        };
    }

    public bool ConfirmExit() => DialogWindow.ConfirmExit(this);

    public ToolbarPosition? PickPosition()
    {
        var picker = new PositionPickerWindow { Owner = this };
        return picker.ShowDialog() == true ? picker.SelectedPosition : null;
    }

    public void NotifyTakesEffectAfterRestart() =>
        DialogWindow.Notify(this, Localizer.Get("StartupChanged_Heading"), Localizer.Get("StartupChanged_Message"));

    private void OnCloseRequested(object? sender, EventArgs e) => Close();

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}

/// <summary>Binds a radio button to one enum value: checked when equal; checking it selects that value.</summary>
public sealed class EnumMatchConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.Equals(value?.ToString(), parameter as string, StringComparison.Ordinal);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Enum.Parse(targetType, (string)parameter) : Binding.DoNothing;
}

public sealed class SoundIconConverter : IValueConverter
{
    public static readonly SoundIconConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Application.Current.FindResource(value is true ? "IconSoundOn" : "IconSoundOff");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
