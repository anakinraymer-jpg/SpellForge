using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using SpellForge.Models;

namespace SpellForge.Views.Controls;

public class MagicCircleCanvas : FrameworkElement
{
    // ── Global zoom hooks (called by menu commands) ───────────────
    public static Action? GlobalZoomIn;
    public static Action? GlobalZoomOut;
    public static Action? GlobalZoomReset;

    // ── Spell dependency property ─────────────────────────────────
    public static readonly DependencyProperty SpellProperty =
        DependencyProperty.Register(nameof(Spell), typeof(Spell), typeof(MagicCircleCanvas),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
                (d, _) => ((MagicCircleCanvas)d).OnSpellChanged()));

    public Spell? Spell
    {
        get => (Spell?)GetValue(SpellProperty);
        set => SetValue(SpellProperty, value);
    }

    private void OnSpellChanged()
    {
        if (Spell != null)
            Spell.PropertyChanged += (_, _) => InvalidateVisual();
        InvalidateVisual();
    }

    // ── Zoom / pan state ──────────────────────────────────────────
    private double _zoom = 1.0;
    private double _ox   = 0;
    private double _oy   = 0;
    private bool   _centered = false;
    private Point? _panStart;

    private const double OuterR  = 370.0;

    public MagicCircleCanvas()
    {
        GlobalZoomIn    = () => ApplyZoom(1.15);
        GlobalZoomOut   = () => ApplyZoom(1.0 / 1.15);
        GlobalZoomReset = () => { _zoom = 1.0; _ox = 0; _oy = 0; _centered = false; InvalidateVisual(); };

        Focusable = true;
        ClipToBounds = true;
    }

    // ── Coordinate helpers ────────────────────────────────────────
    private Point Tc(double wx, double wy) => new(_ox + wx * _zoom, _oy + wy * _zoom);

    // ── Zoom ──────────────────────────────────────────────────────
    private void ApplyZoom(double factor, Point? mouse = null)
    {
        double mx = mouse?.X ?? ActualWidth  / 2;
        double my = mouse?.Y ?? ActualHeight / 2;
        double old = _zoom;
        _zoom = Math.Clamp(_zoom * factor, 0.20, 5.0);
        double scale = _zoom / old;
        _ox = mx - (mx - _ox) * scale;
        _oy = my - (my - _oy) * scale;
        InvalidateVisual();
    }

    // ── Mouse events ──────────────────────────────────────────────
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        ApplyZoom(e.Delta > 0 ? 1.15 : 1.0 / 1.15, e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        _panStart = e.GetPosition(this);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_panStart.HasValue && e.RightButton == MouseButtonState.Pressed)
        {
            var pos = e.GetPosition(this);
            _ox += pos.X - _panStart.Value.X;
            _oy += pos.Y - _panStart.Value.Y;
            _panStart = pos;
            InvalidateVisual();
        }
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        _panStart = null;
        ReleaseMouseCapture();
    }

    // ── Rendering ─────────────────────────────────────────────────
    protected override void OnRender(DrawingContext dc)
    {
        double W = ActualWidth, H = ActualHeight;
        if (W < 1 || H < 1) return;

        if (!_centered)
        {
            _ox = W / 2; _oy = H / 2;
            _centered = true;
        }

        // Parchment background
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x2c, 0x1f, 0x08)), null,
                         new Rect(0, 0, W, H));

        // Radial vignette gradient
        var radial = new RadialGradientBrush(
            Color.FromArgb(0x00, 0x1a, 0x12, 0x08),
            Color.FromArgb(0xCC, 0x0a, 0x06, 0x00))
        {
            Center = new Point(0.5, 0.5),
            RadiusX = 0.7, RadiusY = 0.7,
            GradientOrigin = new Point(0.5, 0.5)
        };
        dc.DrawRectangle(radial, null, new Rect(0, 0, W, H));

        if (Spell == null) return;

        // Placeholder circle until full drawing is implemented
        var origin = Tc(0, 0);
        double rs = OuterR * _zoom;
        var pen = new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0xe8, 0xd8, 0xa0)), 2);
        dc.DrawEllipse(null, pen, origin, rs, rs);

        // Status text
        var ft = new FormattedText(
            $"{Spell.LevelName}  ·  {Spell.TotalPoints} pts  ·  {_zoom:F2}×",
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            11,
            new SolidColorBrush(Color.FromRgb(0x44, 0x55, 0x66)),
            96);
        dc.DrawText(ft, new Point(W / 2 - ft.Width / 2, _oy + OuterR * _zoom + 8));
    }
}
