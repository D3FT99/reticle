using System.Windows;
using System.Windows.Media;

namespace Reticle.Controls;

/// <summary>
/// Circular progress arc starting at 12 o'clock, with round caps, an optional track and an optional outline drawn
/// under the arc so it stays readable over any background without a blur effect.
/// </summary>
public sealed class ProgressRing : FrameworkElement
{
    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        nameof(Progress), typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        nameof(Fill), typeof(Brush), typeof(ProgressRing),
        new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackProperty = DependencyProperty.Register(
        nameof(Track), typeof(Brush), typeof(ProgressRing),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OutlineProperty = DependencyProperty.Register(
        nameof(Outline), typeof(Brush), typeof(ProgressRing),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OutlineThicknessProperty = DependencyProperty.Register(
        nameof(OutlineThickness), typeof(double), typeof(ProgressRing),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>0..1.</summary>
    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public Brush? Track
    {
        get => (Brush?)GetValue(TrackProperty);
        set => SetValue(TrackProperty, value);
    }

    /// <summary>Contrasting edge on both sides of the arc and track.</summary>
    public Brush? Outline
    {
        get => (Brush?)GetValue(OutlineProperty);
        set => SetValue(OutlineProperty, value);
    }

    public double OutlineThickness
    {
        get => (double)GetValue(OutlineThicknessProperty);
        set => SetValue(OutlineThicknessProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        double outline = Outline is null ? 0 : OutlineThickness;
        double radius = Math.Min(ActualWidth, ActualHeight) / 2 - Thickness / 2 - outline;
        if (radius <= 0)
        {
            return;
        }
        var centre = new Point(ActualWidth / 2, ActualHeight / 2);
        Pen? outlinePen = Outline is null
            ? null
            : new Pen(Outline, Thickness + 2 * outline) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };

        if (Track is not null)
        {
            if (outlinePen is not null)
            {
                dc.DrawEllipse(null, outlinePen, centre, radius, radius);
            }
            dc.DrawEllipse(null, new Pen(Track, Thickness), centre, radius, radius);
        }

        double progress = Math.Clamp(Progress, 0, 1);
        if (progress <= 0)
        {
            return;
        }
        Geometry arc = progress >= 0.999 ? new EllipseGeometry(centre, radius, radius) : CreateArc(centre, radius, progress);
        if (outlinePen is not null)
        {
            dc.DrawGeometry(null, outlinePen, arc);
        }
        dc.DrawGeometry(null, new Pen(Fill, Thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, arc);
    }

    private static StreamGeometry CreateArc(Point centre, double radius, double progress)
    {
        double angle = progress * 2 * Math.PI;
        var start = new Point(centre.X, centre.Y - radius);
        var end = new Point(centre.X + radius * Math.Sin(angle), centre.Y - radius * Math.Cos(angle));
        var geometry = new StreamGeometry();
        using (StreamGeometryContext ctx = geometry.Open())
        {
            ctx.BeginFigure(start, isFilled: false, isClosed: false);
            ctx.ArcTo(end, new Size(radius, radius), 0, progress > 0.5, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: false);
        }
        geometry.Freeze();
        return geometry;
    }
}
