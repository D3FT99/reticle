using System.Windows;
using System.Windows.Controls;
using Reticle.Core;

namespace Reticle.Views;

/// <summary>Full-screen picker with eight large arrows for docking the toolbar.</summary>
public partial class PositionPickerWindow : Window
{
    public PositionPickerWindow()
    {
        InitializeComponent();
        Rect area = SystemParameters.WorkArea;
        Left = area.Left;
        Top = area.Top;
        Width = area.Width;
        Height = area.Height;

        // The toolbar would sit on top of the arrows and swallow dwells meant for them.
        var hidden = Application.Current.Windows.OfType<MainWindow>().Where(w => w.IsVisible).ToList();
        Loaded += (_, _) => hidden.ForEach(w => w.Hide());
        Closed += (_, _) => hidden.ForEach(w => w.Show());
    }

    public ToolbarPosition? SelectedPosition { get; private set; }

    private void OnPositionClicked(object sender, RoutedEventArgs e)
    {
        SelectedPosition = ((Button)sender).Tag switch
        {
            "TopLeft" => ToolbarPosition.TopLeft,
            "TopCenter" => ToolbarPosition.TopCenter,
            "TopRight" => ToolbarPosition.TopRight,
            "MiddleLeft" => ToolbarPosition.MiddleLeft,
            "MiddleRight" => ToolbarPosition.MiddleRight,
            "BottomLeft" => ToolbarPosition.BottomLeft,
            "BottomCenter" => ToolbarPosition.BottomCenter,
            "BottomRight" => ToolbarPosition.BottomRight,
            _ => null,
        };
        DialogResult = SelectedPosition is not null;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
