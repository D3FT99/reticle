using System.Windows;
using System.Xml.Linq;
using Reticle.Core;
using Reticle.Engine;
using Xunit;

namespace Reticle.Tests;

public class DwellEngineTests
{
    private static readonly Point Start = new(500, 500);
    private static readonly Point Rest = new(520, 500);

    // DwellTime 1000 → pre-dwell 100: a click needs 1100 ms of rest (plus one frame per state change).
    private const int FullDwell = 1100 + 4 * Harness.Frame;

    [Fact]
    public void Left_click_fires_once_pre_dwell_plus_dwell_time_has_elapsed()
    {
        var h = new Harness();
        h.Wake(Start, Rest);

        h.Hold(Rest, 1050);
        Assert.Empty(h.Mouse.Events);

        h.Hold(Rest, FullDwell - 1050);
        Assert.Equal(["click Left @520,500"], h.Mouse.Events);
    }

    [Fact]
    public void Nothing_happens_until_the_pointer_has_moved()
    {
        var h = new Harness();

        h.Hold(Start, 3 * FullDwell);

        Assert.Empty(h.Mouse.Events);
    }

    [Fact]
    public void Staying_put_after_a_click_does_not_click_again_until_the_pointer_leaves()
    {
        var h = new Harness();
        h.Wake(Start, Rest);

        h.Hold(Rest, 3 * FullDwell);
        Assert.Single(h.Mouse.Events);

        var elsewhere = new Point(700, 300);
        h.Hold(elsewhere, FullDwell);
        Assert.Equal(2, h.Mouse.Events.Count);
        Assert.Equal("click Left @700,300", h.Mouse.Events[1]);
    }

    [Fact]
    public void Leaving_the_target_square_restarts_the_dwell()
    {
        var h = new Harness(targetSize: 60);
        h.Wake(Start, Rest);
        h.Hold(Rest, 900);

        // 40 px is outside the 60 px square (±30) centred on the rest point.
        var drifted = new Point(Rest.X + 40, Rest.Y);
        h.Hold(drifted, 900);
        Assert.Empty(h.Mouse.Events);

        h.Hold(drifted, FullDwell - 900);
        Assert.Equal(["click Left @560,500"], h.Mouse.Events);
    }

    [Fact]
    public void Small_movements_inside_the_target_square_do_not_restart_the_dwell()
    {
        var h = new Harness(targetSize: 60);
        h.Wake(Start, Rest);
        h.Hold(Rest, 600);

        h.Hold(new Point(Rest.X + 20, Rest.Y - 20), FullDwell - 600);

        Assert.Single(h.Mouse.Events);
    }

    [Theory]
    [InlineData(1.0, false)]
    [InlineData(1.5, true)]
    public void Target_size_is_in_DIPs_of_the_monitor_under_the_pointer(double displayScale, bool clicks)
    {
        // 40 DIPs span ±20 px at 100 % and ±30 px at 150 %; a 25 px drift leaves only the first.
        var h = new Harness(targetSize: 40) { DisplayScale = displayScale };
        h.Wake(Start, Rest);
        h.Hold(Rest, 600);

        h.Hold(new Point(Rest.X + 25, Rest.Y), FullDwell - 600);

        Assert.Equal(clicks ? 1 : 0, h.Mouse.Events.Count);
    }

    [Fact]
    public void Rest_mode_never_clicks()
    {
        var h = new Harness();
        h.Engine.ClickType = ClickType.None;
        h.Wake(Start, Rest);

        h.Hold(Rest, 3 * FullDwell);

        Assert.Empty(h.Mouse.Events);
        Assert.True(h.Engine.ClickTypeLocked);
    }

    [Fact]
    public void One_shot_right_click_reverts_to_left_click()
    {
        var h = new Harness();
        h.Engine.ClickType = ClickType.RightClick;
        Assert.False(h.Engine.ClickTypeLocked);
        h.Wake(Start, Rest);

        h.Hold(Rest, FullDwell);

        Assert.Equal(["click Right @520,500"], h.Mouse.Events);
        Assert.Equal(ClickType.LeftClick, h.Engine.ClickType);
    }

    [Fact]
    public void Selecting_a_click_type_twice_locks_it()
    {
        var h = new Harness();
        h.Engine.ClickType = ClickType.DoubleLeftClick;
        h.Engine.ClickType = ClickType.DoubleLeftClick;
        Assert.True(h.Engine.ClickTypeLocked);
        h.Wake(Start, Rest);

        h.Hold(Rest, FullDwell);
        h.Hold(new Point(800, 800), FullDwell);

        Assert.Equal(["double Left @520,500", "double Left @800,800"], h.Mouse.Events);
        Assert.Equal(ClickType.DoubleLeftClick, h.Engine.ClickType);
    }

    [Fact]
    public void Drag_presses_on_first_dwell_releases_on_second_then_reverts()
    {
        var h = new Harness();
        h.Engine.ClickType = ClickType.Drag;
        h.Wake(Start, Rest);

        h.Hold(Rest, FullDwell);
        Assert.True(h.Engine.IsDragging);

        h.Hold(new Point(900, 600), FullDwell);

        Assert.Equal(["press Left @520,500", "release Left @900,600"], h.Mouse.Events);
        Assert.False(h.Engine.IsDragging);
        Assert.Equal(ClickType.LeftClick, h.Engine.ClickType);
    }

    [Fact]
    public void Changing_click_type_mid_drag_releases_the_button()
    {
        var h = new Harness();
        h.Engine.ClickType = ClickType.Drag;
        h.Wake(Start, Rest);
        h.Hold(Rest, FullDwell);

        h.Engine.ClickType = ClickType.None;

        Assert.Equal("release Left @520,500", h.Mouse.Events[^1]);
        Assert.False(h.Engine.IsDragging);
    }

    [Fact]
    public void Click_lands_where_the_pointer_is_without_moving_the_cursor()
    {
        var h = new Harness(targetSize: 60);
        h.Wake(Start, Rest);
        h.Hold(Rest, 900);

        // Drift inside the target square during the last part of the dwell: no snapping back to the settle point.
        var drifted = new Point(Rest.X + 15, Rest.Y + 10);
        h.Hold(drifted, FullDwell - 900);

        Assert.Equal(["click Left @535,510"], h.Mouse.Events);
    }

    [Fact]
    public void Progress_ring_follows_the_pointer_while_dwelling()
    {
        var h = new Harness(targetSize: 60);
        h.Wake(Start, Rest);
        h.Hold(Rest, 600);

        var drifted = new Point(Rest.X - 12, Rest.Y + 8);
        h.Hold(drifted, 100);

        Assert.Equal(drifted, h.Feedback.LastCentre);
    }

    [Fact]
    public void After_a_click_leaving_the_target_square_by_one_pixel_rearms_the_dwell()
    {
        var h = new Harness(targetSize: 60);
        h.Wake(Start, Rest);
        h.Hold(Rest, FullDwell);

        // The square spans ±30 px around the click point; 31 px is just outside it.
        var justOutside = new Point(Rest.X + 31, Rest.Y);
        h.Hold(justOutside, FullDwell);

        Assert.Equal(["click Left @520,500", "click Left @551,500"], h.Mouse.Events);
    }

    [Fact]
    public void Dwelling_on_a_button_invokes_it_instead_of_clicking_the_screen()
    {
        var h = new Harness();
        int invoked = 0;
        var button = new DwellableButton(() => invoked++) { Bounds = new Rect(480, 480, 60, 60) };
        h.Engine.AddButton(button);
        h.Wake(Start, Rest);

        h.Hold(Rest, 3 * FullDwell);

        Assert.Equal(1, invoked);
        Assert.Empty(h.Mouse.Events);
        Assert.Equal(0, button.DwellProgress);
    }

    [Fact]
    public void Button_reports_dwell_progress_while_being_dwelt_on()
    {
        var h = new Harness();
        var button = new DwellableButton(() => { }) { Bounds = new Rect(480, 480, 60, 60) };
        h.Engine.AddButton(button);
        h.Wake(Start, Rest);

        h.Hold(Rest, 100 + 500);

        Assert.InRange(button.DwellProgress, 0.4, 0.6);
    }
}

public class AppSettingsTests
{
    [Fact]
    public void Round_trips_through_xml()
    {
        var settings = AppSettings.InMemory();
        settings.TargetSize = 85;
        settings.DwellTime = 3400;
        settings.SoundEnabled = false;
        settings.Position = ToolbarPosition.BottomRight;
        settings.ButtonSize = 110;
        settings.Theme = AppTheme.Ocean;
        settings.Language = AppLanguage.PortuguesePortugal;

        var copy = AppSettings.InMemory();
        copy.ReadXml(settings.WriteXml().Root!);

        Assert.Equal(85, copy.TargetSize);
        Assert.Equal(110, copy.ButtonSize);
        Assert.Equal(3400, copy.DwellTime);
        Assert.False(copy.SoundEnabled);
        Assert.Equal(ToolbarPosition.BottomRight, copy.Position);
        Assert.Equal(AppTheme.Ocean, copy.Theme);
        Assert.Equal(AppLanguage.PortuguesePortugal, copy.Language);
    }

    [Fact]
    public void Toolbar_size_is_independent_of_target_size()
    {
        var settings = AppSettings.InMemory();
        settings.ButtonSize = 45;

        settings.TargetSize = 100;

        Assert.Equal(45, settings.ButtonSize);
    }

    [Fact]
    public void Out_of_range_or_garbage_values_fall_back_safely()
    {
        var settings = AppSettings.InMemory();
        settings.ReadXml(XElement.Parse(
            "<Settings><TargetSize>5</TargetSize><DwellTime>999999</DwellTime><SoundEnabled>maybe</SoundEnabled>" +
            "<PositionX>Sideways</PositionX><PositionY>Bottom</PositionY><ButtonSize>500</ButtonSize>" +
            "<Theme>Purple</Theme><Language>Klingon</Language></Settings>"));

        Assert.Equal(AppSettings.MinTargetSize, settings.TargetSize);
        Assert.Equal(AppSettings.MaxDwellTime, settings.DwellTime);
        Assert.Equal(AppSettings.MaxButtonSize, settings.ButtonSize);
        Assert.Equal(AppTheme.Midnight, settings.Theme);
        Assert.Equal(AppLanguage.English, settings.Language);
        Assert.True(settings.SoundEnabled);
        Assert.Equal(new ToolbarPosition(HorizontalPosition.Center, VerticalPosition.Bottom), settings.Position);
    }

    [Fact]
    public void Side_positions_stack_the_toolbar_vertically()
    {
        Assert.True(ToolbarPosition.MiddleLeft.IsVertical);
        Assert.True(ToolbarPosition.BottomRight.IsVertical);
        Assert.False(ToolbarPosition.TopCenter.IsVertical);
    }
}
