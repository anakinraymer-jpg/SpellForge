using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SpellForge.Models;

namespace SpellForge.Views.Controls;

public class MagicCircleCanvas : FrameworkElement
{
    // ── Constants ────────────────────────────────────────────────
    private const double OuterR     = 370.0;
    private const double CenterR    = 96.0;
    private const double NodeRPri   = 24.0;
    private const double NodeRSec   = 20.0;
    // Modifier ring: orbit and circle radius are R-relative, defined in DrawModRing
    private const string BgHex      = "#07070e";  // deep space void

    // ── Global zoom actions ──────────────────────────────────────
    public static Action? GlobalZoomIn;
    public static Action? GlobalZoomOut;
    public static Action? GlobalZoomReset;

    // ── Spell DependencyProperty ─────────────────────────────────
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

    // ── Zoom / pan state ─────────────────────────────────────────
    private double _zoom = 1.0;
    private double _ox   = 0;
    private double _oy   = 0;
    private bool   _centered = false;
    private Point? _panStart;
    private bool   _panMoved;

    // ── Hit testing ──────────────────────────────────────────────
    private record HitRegion(Point Center, double Radius, string Left, string Right, Action? OnClick, Action? OnRightClick = null);
    private readonly List<HitRegion> _hits = new();
    private string? _hoverLeft;
    private string? _hoverRight;
    private Point   _mousePos;

    // ── Render-time state (valid only inside OnRender) ────────────
    private DrawingContext _dc = null!;

    private static readonly Typeface _tf      = new("Segoe UI");
    private static readonly Typeface _tfFixed = new("Consolas");
    private static readonly Typeface _tfGeo   = new("Georgia");

    public MagicCircleCanvas()
    {
        GlobalZoomIn    = () => ApplyZoom(1.15);
        GlobalZoomOut   = () => ApplyZoom(1.0 / 1.15);
        GlobalZoomReset = () => { _zoom = 1.0; _ox = 0; _oy = 0; _centered = false; InvalidateVisual(); };
        Focusable    = true;
        ClipToBounds = true;
    }

    // ── Coordinates ──────────────────────────────────────────────
    private Point Tc(double wx, double wy) => new(_ox + wx * _zoom, _oy + wy * _zoom);

    private (double x, double y) Wpt(double wx, double wy, double r, double deg)
    {
        double a = (deg - 90.0) * Math.PI / 180.0;
        return (wx + r * Math.Cos(a), wy + r * Math.Sin(a));
    }

    private Point WptP(double wx, double wy, double r, double deg)
    {
        var (x, y) = Wpt(wx, wy, r, deg);
        return Tc(x, y);
    }

    // ── Zoom ─────────────────────────────────────────────────────
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

    // ── Hit helpers ──────────────────────────────────────────────
    private void MutateSpell(Action<Spell> mutation)
    {
        var s = Spell;
        if (s == null) return;
        mutation(s);
        s.NotifyAllChanged();
    }

    private void Hit(double wx, double wy, double worldR, string left, string right = "", Action? onClick = null, Action? onRightClick = null)
    {
        var sc = Tc(wx, wy);
        _hits.Add(new HitRegion(sc, Math.Max(worldR * _zoom, 12), left, right, onClick, onRightClick));
    }

    // Return the smallest (most specific) matching region so pip hits
    // always win over the larger school-circle hit that contains them.
    private HitRegion? FindHit(Point p) =>
        _hits.Where(h => (p - h.Center).Length <= h.Radius + 4)
             .MinBy(h => h.Radius);

    // ── Mouse ────────────────────────────────────────────────────
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        ApplyZoom(e.Delta > 0 ? 1.15 : 1.0 / 1.15, e.GetPosition(this));
        e.Handled = true;
    }
    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        _panStart = e.GetPosition(this);
        _panMoved = false;
        CaptureMouse();
    }
    protected override void OnMouseMove(MouseEventArgs e)
    {
        _mousePos = e.GetPosition(this);

        if (_panStart.HasValue && e.RightButton == MouseButtonState.Pressed)
        {
            double ddx = _mousePos.X - _panStart.Value.X;
            double ddy = _mousePos.Y - _panStart.Value.Y;
            if (Math.Sqrt(ddx * ddx + ddy * ddy) >= 5)
            {
                _panMoved  = true;
                _ox       += ddx;
                _oy       += ddy;
                _panStart  = _mousePos;
                _hoverLeft = null; _hoverRight = null;
                InvalidateVisual();
            }
            return;
        }

        var hit      = FindHit(_mousePos);
        var newLeft  = hit?.Left;
        var newRight = hit?.Right;
        if (newLeft != _hoverLeft || newRight != _hoverRight)
        {
            _hoverLeft  = newLeft;
            _hoverRight = newRight;
            InvalidateVisual();
        }
        else if (_hoverLeft != null)
        {
            InvalidateVisual(); // keep tracking cursor for crosshair
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();
        var hit = FindHit(e.GetPosition(this));
        if (hit?.OnClick != null)
        {
            hit.OnClick();
            e.Handled = true;
            return;
        }
        base.OnMouseLeftButtonDown(e);
    }

    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        if (!_panMoved)
        {
            Focus();
            var hit = FindHit(e.GetPosition(this));
            if (hit?.OnRightClick != null)
            {
                hit.OnRightClick();
                e.Handled = true;
            }
        }
        _panStart = null;
        ReleaseMouseCapture();
    }

    // ════════════════════════════════════════════════════════════
    //  COLOR HELPERS
    // ════════════════════════════════════════════════════════════

    // Blend two hex colors → Color
    private static Color B(string c1, string c2, double t)
        => ColorHelper.Blend(ColorHelper.FromHex(c1), ColorHelper.FromHex(c2), t);

    // Fade: BG → color at alpha → Color
    private static Color F(string c, double alpha)
        => ColorHelper.Blend(ColorHelper.FromHex(BgHex), ColorHelper.FromHex(c), alpha);

    private static SolidColorBrush Br(Color c)                      => new(c);
    private static SolidColorBrush Br(string hex)                   => new(ColorHelper.FromHex(hex));
    private static SolidColorBrush Br(string hex, double alpha)     => new(F(hex, alpha));
    private static SolidColorBrush BrB(string c1, string c2, double t) => new(B(c1, c2, t));

    private static Pen Pn(Color c, double w, bool dash = false)
    {
        var p = new Pen(new SolidColorBrush(c), w);
        if (dash) p.DashStyle = new DashStyle(new[] { 4.0, 4.0 }, 0);
        return p;
    }
    private static Pen Pn(string hex, double w, bool dash = false)
        => Pn(ColorHelper.FromHex(hex), w, dash);
    private static Pen PnF(string hex, double alpha, double w, bool dash = false)
        => Pn(F(hex, alpha), w, dash);
    private static Pen PnB(string c1, string c2, double t, double w, bool dash = false)
        => Pn(B(c1, c2, t), w, dash);

    // ════════════════════════════════════════════════════════════
    //  DRAWING PRIMITIVES  (all take world-space coordinates)
    // ════════════════════════════════════════════════════════════

    private void CircleW(double wx, double wy, double r, Brush? fill = null, Pen? stroke = null)
        => _dc.DrawEllipse(fill, stroke, Tc(wx, wy), r * _zoom, r * _zoom);

    private void RingW(double wx, double wy, double r, Color c, double w = 1, bool dash = false)
        => CircleW(wx, wy, r, null, Pn(c, w, dash));
    private void RingW(double wx, double wy, double r, string hex, double w = 1, bool dash = false)
        => RingW(wx, wy, r, ColorHelper.FromHex(hex), w, dash);
    private void RingWF(double wx, double wy, double r, string hex, double alpha, double w = 1, bool dash = false)
        => RingW(wx, wy, r, F(hex, alpha), w, dash);
    private void RingWB(double wx, double wy, double r, string c1, string c2, double t, double w = 1)
        => RingW(wx, wy, r, B(c1, c2, t), w);

    private void LineW(double x1, double y1, double x2, double y2, Pen pen)
        => _dc.DrawLine(pen, Tc(x1, y1), Tc(x2, y2));
    private void LineW(double x1, double y1, double x2, double y2, string hex, double w = 1)
        => LineW(x1, y1, x2, y2, Pn(hex, w));
    private void LineWF(double x1, double y1, double x2, double y2, string hex, double alpha, double w = 1)
        => LineW(x1, y1, x2, y2, PnF(hex, alpha, w));

    private void QuadBezW(double x1, double y1, double cx, double cy,
                           double x2, double y2, Pen pen)
    {
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(Tc(x1, y1), false, false);
            ctx.QuadraticBezierTo(Tc(cx, cy), Tc(x2, y2), true, false);
        }
        geom.Freeze();
        _dc.DrawGeometry(null, pen, geom);
    }

    private void PolyW(IEnumerable<(double, double)> worldPts, Brush? fill = null, Pen? stroke = null)
    {
        var pts = worldPts.Select(p => Tc(p.Item1, p.Item2)).ToList();
        if (pts.Count < 3) return;
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(pts[0], fill != null, true);
            ctx.PolyLineTo(pts.Skip(1).ToList(), stroke != null, false);
        }
        geom.Freeze();
        _dc.DrawGeometry(fill, stroke, geom);
    }
    private void PolyWF(IEnumerable<(double, double)> pts, string hex, double alpha)
        => PolyW(pts, Br(hex, alpha));

    private void TextW(double wx, double wy, string text, Brush brush, Typeface tf, double size, double angle = 0)
    {
        if (string.IsNullOrEmpty(text)) return;
        var ft = new FormattedText(text, CultureInfo.CurrentCulture,
                                   FlowDirection.LeftToRight, tf, size * _zoom, brush, 1.0);
        var c = Tc(wx, wy);
        var origin = new Point(c.X - ft.Width / 2, c.Y - ft.Height / 2);
        if (Math.Abs(angle) > 0.01)
        {
            _dc.PushTransform(new RotateTransform(angle, c.X, c.Y));
            _dc.DrawText(ft, origin);
            _dc.Pop();
        }
        else
            _dc.DrawText(ft, origin);
    }
    private void TextW(double wx, double wy, string text, string hex, double size,
                       bool isFixed = false, double angle = 0)
        => TextW(wx, wy, text, Br(hex), isFixed ? _tfFixed : _tf, size, angle);
    private void TextWF(double wx, double wy, string text, string hex, double alpha, double size,
                        bool isFixed = false, double angle = 0)
        => TextW(wx, wy, text, Br(hex, alpha), isFixed ? _tfFixed : _tf, size, angle);
    private void TextWB(double wx, double wy, string text, string c1, string c2, double t, double size,
                        bool isFixed = false, double angle = 0)
        => TextW(wx, wy, text, BrB(c1, c2, t), isFixed ? _tfFixed : _tf, size, angle);

    private void ArcRingW(double wx, double wy, double r, double startDeg, double extentDeg, Pen pen)
    {
        if (Math.Abs(extentDeg) < 0.01) return;
        double rs = r * _zoom;
        if (Math.Abs(extentDeg) >= 359.9)
        {
            _dc.DrawEllipse(null, pen, Tc(wx, wy), rs, rs);
            return;
        }
        bool large   = Math.Abs(extentDeg) > 180;
        var startPt  = WptP(wx, wy, r, startDeg);
        var endPt    = WptP(wx, wy, r, startDeg + extentDeg);
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(startPt, false, false);
            ctx.ArcTo(endPt, new Size(rs, rs), 0, large, SweepDirection.Clockwise, true, false);
        }
        geom.Freeze();
        _dc.DrawGeometry(null, pen, geom);
    }
    private void ArcRingW(double wx, double wy, double r, double start, double extent, string hex, double w = 1)
        => ArcRingW(wx, wy, r, start, extent, Pn(hex, w));
    private void ArcRingWF(double wx, double wy, double r, double start, double extent,
                           string hex, double alpha, double w = 1)
        => ArcRingW(wx, wy, r, start, extent, PnF(hex, alpha, w));

    private void WedgeW(double wx, double wy, double rIn, double rOut,
                        double dStart, double dEnd, Brush fill)
    {
        double span = dEnd - dStart;
        if (Math.Abs(span) < 0.01) return;
        bool large   = Math.Abs(span) > 180;
        double rInS  = rIn  * _zoom;
        double rOutS = rOut * _zoom;
        var oStart = WptP(wx, wy, rOut, dStart);
        var oEnd   = WptP(wx, wy, rOut, dEnd);
        var iStart = WptP(wx, wy, rIn,  dStart);
        var iEnd   = WptP(wx, wy, rIn,  dEnd);
        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            ctx.BeginFigure(oStart, true, true);
            ctx.ArcTo(oEnd,   new Size(rOutS, rOutS), 0, large, SweepDirection.Clockwise,        true, false);
            ctx.LineTo(iEnd,  true, false);
            ctx.ArcTo(iStart, new Size(rInS,  rInS),  0, large, SweepDirection.Counterclockwise, true, false);
        }
        geom.Freeze();
        _dc.DrawGeometry(fill, null, geom);
    }
    private void WedgeWF(double wx, double wy, double rIn, double rOut,
                         double dStart, double dEnd, string hex, double alpha)
        => WedgeW(wx, wy, rIn, rOut, dStart, dEnd, Br(hex, alpha));

    private void PolyNW(double wx, double wy, double r, int n, double off = 0,
                        Brush? fill = null, Pen? stroke = null)
        => PolyW(Enumerable.Range(0, n).Select(i => Wpt(wx, wy, r, i * (360.0 / n) + off)),
                 fill, stroke);

    private void StarW(double wx, double wy, double rOut, double rIn, int n, double off = 0,
                       Brush? fill = null, Pen? stroke = null)
    {
        var pts = new List<(double, double)>();
        for (int i = 0; i < n; i++)
        {
            pts.Add(Wpt(wx, wy, rOut, i * (360.0 / n) + off));
            pts.Add(Wpt(wx, wy, rIn,  i * (360.0 / n) + (180.0 / n) + off));
        }
        PolyW(pts, fill, stroke);
    }

    private void ArcTextW(double wx, double wy, double r, string text, double startDeg,
                          string hexColor, double fontSize = 6, double stepDeg = 4.5)
    {
        if (string.IsNullOrEmpty(text)) return;
        double total = text.Length * stepDeg;
        double angle = startDeg - total / 2;
        var brush = Br(hexColor);
        foreach (char ch in text)
        {
            var (ax, ay) = Wpt(wx, wy, r, angle);
            TextW(ax, ay, ch.ToString(), brush, _tfGeo, fontSize, angle - 90);
            angle += stepDeg;
        }
    }

    // ════════════════════════════════════════════════════════════
    //  MAIN RENDER
    // ════════════════════════════════════════════════════════════

    protected override void OnRender(DrawingContext dc)
    {
        _dc = dc;
        _hits.Clear();
        double W = ActualWidth, H = ActualHeight;
        if (W < 1 || H < 1) return;

        if (!_centered) { _ox = W / 2; _oy = H / 2; _centered = true; }

        double R = OuterR;
        DrawDeepBg(R, W, H);
        if (Spell == null) return;

        var pos = ComputeNodePositions(R);
        DrawOuterFrame(R, pos);
        DrawMainGeometry(R);
        DrawElementRing(R);
        DrawModRing(R);       // modifier circles on middle ring (R×0.38 track)
        DrawSchoolModules(pos);
        DrawCenterHub();
        DrawDrawbackRings();
        DrawConditionsRing();
        DrawStatusBar();
        DrawHoverInfo();
    }

    // ════════════════════════════════════════════════════════════
    //  HOVER INFO BOX  (screen-space overlay, drawn last)
    // ════════════════════════════════════════════════════════════

    private void DrawHoverInfo()
    {
        if (string.IsNullOrEmpty(_hoverLeft)) return;

        double W = ActualWidth, H = ActualHeight;

        // ── Cursor crosshair exactly at mouse tip ─────────────────
        var glowPen = new Pen(new SolidColorBrush(Color.FromArgb(0xCC, 0xff, 0xee, 0x55)), 1.2);
        double cr = 6;
        _dc.DrawEllipse(null,
            new Pen(new SolidColorBrush(Color.FromArgb(0x88, 0xff, 0xee, 0x55)), 1.5),
            _mousePos, cr, cr);
        _dc.DrawLine(glowPen,
            new Point(_mousePos.X - cr - 3, _mousePos.Y),
            new Point(_mousePos.X + cr + 3, _mousePos.Y));
        _dc.DrawLine(glowPen,
            new Point(_mousePos.X, _mousePos.Y - cr - 3),
            new Point(_mousePos.X, _mousePos.Y + cr + 3));

        // ── Build column texts ────────────────────────────────────
        const double colMaxW = 240, padX = 12, padY = 9, divPad = 8, divW = 1;

        string[] leftLines  = _hoverLeft!.Split('\n');
        bool     hasRight   = !string.IsNullOrEmpty(_hoverRight);
        string[] rightLines = hasRight ? _hoverRight!.Split('\n') : Array.Empty<string>();

        var (leftFts, lColW, lColH)   = BuildColTexts(leftLines,  colMaxW, isLeft: true);
        var (rightFts, rColW, rColH)  = hasRight
            ? BuildColTexts(rightLines, colMaxW, isLeft: false)
            : (Array.Empty<FormattedText>(), 0.0, 0.0);

        double boxW = hasRight
            ? padX + lColW + divPad + divW + divPad + rColW + padX
            : padX + lColW + padX;
        double boxH = Math.Max(lColH, rColH) + padY * 2;

        // Center-bottom
        double bx = Math.Clamp((W - boxW) / 2, 4, W - boxW - 4);
        double by = Math.Clamp(H - boxH - 10, 4, H - boxH - 4);

        // Box background + border
        _dc.DrawRoundedRectangle(
            new SolidColorBrush(Color.FromArgb(0xF2, 0x06, 0x06, 0x11)),
            new Pen(new SolidColorBrush(Color.FromRgb(0x44, 0x66, 0xff)), 1),
            new Rect(bx, by, boxW, boxH), 6, 6);

        // Left column
        double ty = by + padY;
        for (int i = 0; i < leftFts.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(leftLines[i]))
                _dc.DrawText(leftFts[i], new Point(bx + padX, ty));
            ty += leftFts[i].Height + 2;
        }

        if (hasRight)
        {
            // Divider line
            double divX = bx + padX + lColW + divPad;
            _dc.DrawLine(
                new Pen(new SolidColorBrush(Color.FromArgb(0x55, 0x66, 0x88, 0xff)), 1),
                new Point(divX, by + padY * 0.5),
                new Point(divX, by + boxH - padY * 0.5));

            // Right column
            double rx = divX + divW + divPad;
            ty = by + padY;
            for (int i = 0; i < rightFts.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(rightLines[i]))
                    _dc.DrawText(rightFts[i], new Point(rx, ty));
                ty += rightFts[i].Height + 2;
            }
        }
    }

    private (FormattedText[] fts, double maxW, double totalH) BuildColTexts(
        string[] lines, double colMaxW, bool isLeft)
    {
        var fts = new FormattedText[lines.Length];
        double maxW = 0, totalH = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            bool hdr = i == 0;
            var color = hdr
                ? (isLeft
                    ? Color.FromRgb(0xff, 0xee, 0x88)   // gold header for pip info
                    : Color.FromRgb(0x88, 0xcc, 0xff))  // blue header for circle context
                : Color.FromRgb(0xc8, 0xd0, 0xe8);
            fts[i] = new FormattedText(
                string.IsNullOrWhiteSpace(lines[i]) ? " " : lines[i],
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                hdr ? _tfGeo : _tf, hdr ? 12 : 9.5,
                new SolidColorBrush(color), 1.0)
            { MaxTextWidth = colMaxW };
            maxW   = Math.Max(maxW, Math.Min(fts[i].Width + 4, colMaxW));
            totalH += fts[i].Height + 2;
        }
        return (fts, maxW, totalH);
    }

    // ════════════════════════════════════════════════════════════
    //  LAYER 1 — PARCHMENT BACKGROUND
    // ════════════════════════════════════════════════════════════

    private void DrawDeepBg(double R, double W, double H)
    {
        // ── Deep void base ────────────────────────────────────────
        _dc.DrawRectangle(Br(BgHex), null, new Rect(0, 0, W, H));

        // Subtle radial brightening toward the circle centre
        const string mid = "#0e0e1c";
        for (int i = 12; i >= 1; i--)
            CircleW(0, 0, R * 1.55 * i / 12, Br(B(BgHex, mid, (1.0 - (double)i / 12) * 0.45)));

        // Faint containment rings (cool blue-violet)
        foreach (var (frac, sw, alpha) in new[] {
            (1.50, 4.0, 0.30), (1.28, 2.0, 0.20), (1.08, 3.0, 0.15) })
            RingW(0, 0, R * frac, F("#2233aa", alpha), sw);

        // ── Star field (screen-space, pans/zooms independently) ──
        var rng = new Random(17);
        for (int i = 0; i < 90; i++)
        {
            double sx = rng.NextDouble() * W;
            double sy = rng.NextDouble() * H;
            double sr = 0.3 + rng.NextDouble() * 1.4;
            byte   sa = (byte)(25 + rng.Next(0, 140));
            byte   br = (byte)(160 + rng.Next(0, 60));
            _dc.DrawEllipse(
                new SolidColorBrush(Color.FromArgb(sa,
                    (byte)Math.Max(0, br - 20), br, (byte)Math.Min(255, br + 30))),
                null, new Point(sx, sy), sr, sr);
        }

        // ── Inner vignette rings (cool tint, not warm) ───────────
        foreach (var (frac, alpha) in new[] { (0.88, 0.07), (0.65, 0.05), (0.40, 0.04) })
            RingWF(0, 0, R * frac, "#2244aa", alpha);
    }

    // ════════════════════════════════════════════════════════════
    //  LAYER 2 — OUTER FRAME
    // ════════════════════════════════════════════════════════════

    // Theme centres are 72° apart; 5 schools within each theme spread ±20° at 10° steps.
    private static readonly string[] _themeNames  = ["Dark","Light","Nature","Planar","Arcane"];
    private static readonly double[] _themeAngles = [0.0, 72.0, 144.0, 216.0, 288.0];
    private static readonly double[] _themeOffsets = [-20.0, -10.0, 0.0, 10.0, 20.0];

    // Representative color and symbol for each theme
    private static readonly string[] _themeColors  =
        ["#bb1133", "#ccbb33", "#338833", "#3355bb", "#4488aa"];
    private static readonly string[] _themeSymbols =
        ["☠", "✦", "⚘", "⌛", "ᚠ"];

    private Dictionary<string, (double x, double y)> ComputeNodePositions(double R)
    {
        var pos = new Dictionary<string, (double, double)>();
        double nodeR = R * 0.82;
        for (int t = 0; t < _themeNames.Length; t++)
        {
            var schools = GameData.SchoolThemes[_themeNames[t]];
            for (int s = 0; s < schools.Count; s++)
                pos[schools[s]] = Wpt(0, 0, nodeR, _themeAngles[t] + _themeOffsets[s]);
        }
        return pos;
    }

    private void DrawOuterFrame(double R, Dictionary<string, (double x, double y)> pos)
    {
        var s      = Spell!;
        var lvl    = s.LevelInfo;
        var active = new HashSet<string>(s.AllSchools);

        // Cool silver tones for dark-void background
        string ink  = "#c0cce0";
        string ink2 = "#3d5070";

        double schoolR = R * 0.82;

        // ── Theme sector fills (drawn first, behind rings) ────────
        for (int t = 0; t < _themeNames.Length; t++)
        {
            string tc   = _themeColors[t];
            double tA   = _themeAngles[t];
            double aS   = tA - 24.0;
            const double span = 48.0;
            bool anyActive = GameData.SchoolThemes[_themeNames[t]].Any(sc => active.Contains(sc));

            // Faint wedge fill in the school ring zone for this theme
            WedgeWF(0, 0, schoolR * 0.93, schoolR + NodeRSec * 2.0, aS, aS + span, tc,
                    anyActive ? 0.11 : 0.04);

            // Colored arc on the outer label ring
            ArcRingWF(0, 0, schoolR + NodeRSec * 1.85, aS, span, tc, anyActive ? 0.80 : 0.40, 2);

            // Theme label in arc text
            ArcTextW(0, 0, schoolR + NodeRSec * 2.60, _themeNames[t].ToUpper(),
                     tA, tc, anyActive ? 5.5 : 4.5, 5.2);

            // Theme separator radial lines at sector edges
            foreach (double boundary in new[] { aS, aS + span })
            {
                var (bx1, by1) = Wpt(0, 0, schoolR * 0.92, boundary);
                var (bx2, by2) = Wpt(0, 0, R * 0.975,      boundary);
                LineWF(bx1, by1, bx2, by2, tc, 0.35, 1);
            }
        }

        // ── Outer border rings ────────────────────────────────────
        RingW(0, 0, R,         ink,  3);
        RingW(0, 0, R * 0.978, ink2, 1);

        // ── Tick marks (colored per theme zone) ──────────────────
        for (int i = 0; i < 72; i++)
        {
            double a  = i * 5.0;
            double sw = i % 6 == 0 ? 3.0 : 1.0;
            double r1 = R * (i % 6 == 0 ? 0.956 : 0.966);
            var (x1, y1) = Wpt(0, 0, r1,        a);
            var (x2, y2) = Wpt(0, 0, R * 0.978, a);
            string tickC = ThemeColorAt(a, ink2);
            LineW(x1, y1, x2, y2, tickC, sw);
        }

        // ── School ring ───────────────────────────────────────────
        RingW(0, 0, schoolR, ink2, 2);

        // ── Within-theme chord lines (each theme's color) ─────────
        for (int t = 0; t < _themeNames.Length; t++)
        {
            string tc   = _themeColors[t];
            var schools = GameData.SchoolThemes[_themeNames[t]];
            var spts    = schools.Select(sc => pos[sc]).ToArray();
            for (int i = 0; i < spts.Length; i++)
                for (int j = i + 1; j < spts.Length; j++)
                    LineWF(spts[i].x, spts[i].y, spts[j].x, spts[j].y, tc, 0.14);
        }

        // ── Cross-theme centre-to-centre lines (very faint) ───────
        for (int t1 = 0; t1 < _themeNames.Length; t1++)
        for (int t2 = t1 + 1; t2 < _themeNames.Length; t2++)
        {
            var (cx1, cy1) = Wpt(0, 0, schoolR, _themeAngles[t1]);
            var (cx2, cy2) = Wpt(0, 0, schoolR, _themeAngles[t2]);
            LineWF(cx1, cy1, cx2, cy2, ink2, 0.05);
        }

        // ── Diamond tick at each school's radial position ─────────
        foreach (var (school, (sx, sy)) in pos)
        {
            double ang   = Math.Atan2(sy, sx) * 180.0 / Math.PI + 90.0;
            var (mx, my) = Wpt(0, 0, schoolR + NodeRSec * 1.95, ang);
            string c2    = GameData.Schools[school].Color;
            string fill  = active.Contains(school) ? c2 : ColorHelper.Blend(BgHex, c2, 0.28);
            TextW(mx, my, "◆", fill, 5);
        }

        // ── Spell name arc text ────────────────────────────────────
        if (!string.IsNullOrEmpty(s.Name))
            ArcTextW(0, 0, R * 0.990, $" ✦ {s.Name.ToUpper()} ✦ {lvl.Name.ToUpper()} ✦ ",
                     0, lvl.Color, 6, 3.8);

        DrawConnectionLines(pos);
    }

    // Returns the theme color if angleDeg falls within any 48° theme sector, else fallback.
    private static string ThemeColorAt(double angleDeg, string fallback)
    {
        angleDeg = ((angleDeg % 360) + 360) % 360;
        for (int t = 0; t < _themeAngles.Length; t++)
        {
            double diff = Math.Abs(((angleDeg - _themeAngles[t] + 540) % 360) - 180);
            if (diff <= 24.0) return _themeColors[t];
        }
        return fallback;
    }

    private void DrawConnectionLines(Dictionary<string, (double x, double y)> pos)
    {
        var s       = Spell!;
        var schools = s.AllSchools;
        var seen    = new HashSet<string>();

        for (int i = 0; i < schools.Count; i++)
        for (int j = i + 1; j < schools.Count; j++)
        {
            var pair = GameData.SchoolPair(schools[i], schools[j]);
            if (pair == null) continue;
            string key = $"{pair.Value.Item1}|{pair.Value.Item2}";
            if (!seen.Add(key)) continue;

            bool cap = s.CapstoneActive(schools[i]) && s.CapstoneActive(schools[j]);
            var (x1, y1) = pos[schools[i]];
            var (x2, y2) = pos[schools[j]];
            string mid   = ColorHelper.Blend(GameData.Schools[schools[i]].Color,
                                              GameData.Schools[schools[j]].Color, 0.5);

            // Arc bows inward — guaranteed midpoint at 75% of ring radius
            double nodeRing = OuterR * 0.82;
            double ang1 = Math.Atan2(y1, x1);
            double ang2 = Math.Atan2(y2, x2);
            double da   = ang2 - ang1;
            if (da >  Math.PI) da -= 2 * Math.PI;
            if (da < -Math.PI) da += 2 * Math.PI;
            double avgAng = ang1 + da / 2.0;
            // Desired curve midpoint: angular bisector, 25% inside the school ring
            double mx = Math.Cos(avgAng) * nodeRing * 0.75;
            double my = Math.Sin(avgAng) * nodeRing * 0.75;
            // Back-solve for quadratic Bézier control so that B(0.5) == (mx, my):
            //   B(0.5) = 0.25·A + 0.5·ctrl + 0.25·B  =>  ctrl = 2·M − 0.5·(A+B)
            double ctrlX = 2 * mx - 0.5 * (x1 + x2);
            double ctrlY = 2 * my - 0.5 * (y1 + y2);
            QuadBezW(x1, y1, ctrlX, ctrlY, x2, y2,
                     cap ? PnF("#FFD700", 0.55, 2, true) : PnF(mid, 0.30, 2));
            int seed    = ((schools[i] + schools[j]).Sum(c => (int)c) % GameData.RunesAll.Length
                           + GameData.RunesAll.Length) % GameData.RunesAll.Length;
            TextWF(mx, my, GameData.RunesAll[seed].ToString(),
                   cap ? "#FFD700" : mid, 0.70, 6, isFixed: true);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  LAYER 3 — INNER GEOMETRY
    // ════════════════════════════════════════════════════════════

    private void DrawMainGeometry(double R)
    {
        var asc   = Spell!.AllSchools;
        double webR = R * 0.48;

        // Which themes are active?
        var activeTheme = new bool[5];
        for (int t = 0; t < _themeNames.Length; t++)
            activeTheme[t] = GameData.SchoolThemes[_themeNames[t]].Any(asc.Contains);

        // Blend of all active school colors → neutral geometry tint
        string blendC = asc.Count > 0
            ? asc.Aggregate("#334466",
                  (acc, sc) => ColorHelper.Blend(acc, GameData.Schools[sc].Color, 0.35))
            : "#334466";

        // Outer glow boundary ring
        RingWF(0, 0, R * 0.50, blendC, 0.16);

        // ── 5-point pentagram — vertices align with theme centres ──
        var verts = _themeAngles.Select(a => Wpt(0, 0, webR, a)).ToArray();

        // Pentagon filled backdrop
        PolyW(verts, Br(blendC, 0.04));

        // Pentagram chord web (every vertex to every other)
        for (int i = 0; i < 5; i++)
        for (int j = i + 1; j < 5; j++)
        {
            string edgeC = ColorHelper.Blend(_themeColors[i], _themeColors[j], 0.5);
            bool either  = activeTheme[i] || activeTheme[j];
            LineWF(verts[i].x, verts[i].y, verts[j].x, verts[j].y, edgeC, either ? 0.16 : 0.06);
        }

        // Pentagon outline (adjacent pairs only, slightly brighter)
        for (int i = 0; i < 5; i++)
        {
            int j = (i + 1) % 5;
            string edgeC = ColorHelper.Blend(_themeColors[i], _themeColors[j], 0.5);
            LineWF(verts[i].x, verts[i].y, verts[j].x, verts[j].y, edgeC, 0.28);
        }

        // Concentric rings, fading inward
        foreach (var (frac, alpha) in new[] { (0.48, 0.15), (0.38, 0.11), (0.28, 0.09), (0.18, 0.07) })
            RingWF(0, 0, R * frac, blendC, alpha);

        // Inner triangle pair (hexagram)
        PolyNW(0, 0, R * 0.42, 3,     stroke: PnF(blendC, 0.22, 1));
        PolyNW(0, 0, R * 0.42, 3, 60, stroke: PnF(blendC, 0.16, 1, true));

        // ── Theme vertex markers — glow when theme is active ──────
        for (int t = 0; t < 5; t++)
        {
            string tc  = _themeColors[t];
            bool   act = activeTheme[t];
            // Vertex dot
            CircleW(verts[t].x, verts[t].y, act ? 5.0 : 2.0, Br(tc, act ? 0.92 : 0.28));
            if (act)
            {
                // Glow halo + theme symbol
                RingWF(verts[t].x, verts[t].y, 9.0, tc, 0.50, 1);
                RingWF(verts[t].x, verts[t].y, 14.0, tc, 0.18, 1, true);
                TextWF(verts[t].x, verts[t].y, _themeSymbols[t], tc, 0.85, 6);
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    //  LAYER 4 — ELEMENT RING
    // ════════════════════════════════════════════════════════════

    private void DrawElementRing(double R)
    {
        var s      = Spell!;
        var active = s.Elements.Where(kv => kv.Value != null).ToList();
        if (active.Count == 0) return;

        double rIn   = R * 0.545, rOut = R * 0.725;
        double rMid  = (rIn + rOut) * 0.5;
        double rName = rOut - 8, rRune = rIn + 10, rNode = rMid - 6;
        int    n     = active.Count;
        double sec   = 360.0 / n;
        double gap   = n > 1 ? 1.5 : 0.0;

        string border = "#b0c0d8";   // cool silver for dark background
        RingW(0, 0, rIn,  border, 2);
        RingW(0, 0, rOut, border, 2);

        var angMap = new Dictionary<string, (double s, double e, double m)>();

        for (int i = 0; i < n; i++)
        {
            string el   = active[i].Key;
            string? val = active[i].Value;
            double aS   = i * sec + gap / 2;
            double aE   = (i + 1) * sec - gap / 2;
            double aM   = (aS + aE) * 0.5;
            angMap[el]  = (aS, aE, aM);
            double span = aE - aS;

            var edata = GameData.Elements[el];
            string ec = (el == "Celestial" && val is { Length: > 0 }
                         && edata.Subtypes != null && edata.Subtypes.ContainsKey(val))
                        ? edata.Subtypes[val].Color : edata.Color;

            WedgeWF(0, 0, rIn, rOut, aS, aE, ec, 0.20);
            ArcRingWF(0, 0, rIn,  aS, span, ec, 0.50, 2);
            ArcRingWF(0, 0, rOut, aS, span, ec, 0.50, 2);

            if (n > 1)
            {
                foreach (double sep in new[] { aS, aE })
                {
                    var (lx1, ly1) = Wpt(0, 0, rIn,  sep);
                    var (lx2, ly2) = Wpt(0, 0, rOut, sep);
                    LineW(lx1, ly1, lx2, ly2, PnB(border, ec, 0.45, 2));
                }
            }

            var (sx, sy) = Wpt(0, 0, rMid, aM);
            CircleW(sx, sy, 11, Br(ec, 0.35));
            RingWB(sx, sy, 11, ec, "#ffffff", 0.55);
            TextWB(sx, sy, edata.Symbol, ec, "#ffffff", 0.90, 10);

            double step = Math.Min(4.5, Math.Max(1.8, (span - 6) / Math.Max(el.Length, 1)));
            ArcTextW(0, 0, rName, el.ToUpper(), aM, ec, 6, step);

            int elSeed  = ((el.Sum(c => (int)c) % GameData.RunesAll.Length)
                          + GameData.RunesAll.Length) % GameData.RunesAll.Length;
            int nRunes  = Math.Max(1, Math.Min(10, (int)(span / 10)));
            for (int j = 0; j < nRunes; j++)
            {
                double ra = nRunes == 1 ? aM : aS + gap + j * (span - 2 * gap) / (nRunes - 1);
                var (rrx, rry) = Wpt(0, 0, rRune, ra);
                int rseed = (elSeed + j * 13) % GameData.RunesAll.Length;
                TextWF(rrx, rry, GameData.RunesAll[rseed].ToString(), ec, 0.40, 5, isFixed: true);
            }

            s.ElementNodes.TryGetValue(el, out var boughtNodes); boughtNodes ??= new();
            int nn = edata.Nodes.Count;
            if (nn > 0)
            {
                double half = span * 0.35;
                for (int k = 0; k < nn; k++)
                {
                    double offset = nn > 1 ? -half + k * (2 * half / (nn - 1)) : 0;
                    var (nwx, nwy) = Wpt(0, 0, rNode, aM + offset);
                    bool bought = boughtNodes.TryGetValue(edata.Nodes[k].Name, out int bc) && bc > 0;
                    var capEl    = el; var capNode = edata.Nodes[k]; var capBc = bc;
                    var capEdata = edata;
                    string capEnKey   = $"elemnode/{capEl}/{capNode.Name}";
                    bool   enDrawback = bought && s.DrawbackBuys.ContainsKey(capEnKey);
                    DrawElemNode(nwx, nwy, capNode.Glyph, ec, bought ? 6 : 4, bought, enDrawback);
                    string enLeft = enDrawback
                        ? $"{capNode.Glyph} {capNode.Name}  ({capNode.Cost} pt)  ⚠ Drawback\nLevel: {capBc}/3\n{capNode.Desc}\nRefunds −{capNode.Cost} pt  ·  Right-click to remove"
                        : bought
                            ? $"{capNode.Glyph} {capNode.Name}  ({capNode.Cost} pt)\nLevel: {capBc}/3\n{capNode.Desc}\nRight-click: mark as drawback"
                            : $"{capNode.Glyph} {capNode.Name}  ({capNode.Cost} pt)\nLevel: {capBc}/3\n{capNode.Desc}";
                    Action? enRC = bought
                        ? () => MutateSpell(sp => {
                            if (sp.DrawbackBuys.ContainsKey(capEnKey)) sp.DrawbackBuys.Remove(capEnKey);
                            else sp.DrawbackBuys[capEnKey] = capNode.Name;
                          })
                        : null;
                    Hit(nwx, nwy, bought ? 6 : 4,
                        enLeft,
                        $"Element: {capEdata.Symbol} {capEl}\n{capEdata.Desc}",
                        () => MutateSpell(sp => {
                            if (!sp.ElementNodes.ContainsKey(capEl)) sp.ElementNodes[capEl] = new();
                            int cur = sp.ElementNodes[capEl].TryGetValue(capNode.Name, out int v) ? v : 0;
                            int nv  = cur >= 3 ? 0 : cur + 1;
                            sp.ElementNodes[capEl][capNode.Name] = nv;
                            if (nv == 0) sp.DrawbackBuys.Remove(capEnKey);
                        }),
                        enRC);
                }
            }
        }

        if (n <= 1) return;
        for (int i = 0; i < n; i++)
        {
            string el1 = active[i].Key;
            string el2 = active[(i + 1) % n].Key;
            (string, string)? pair =
                GameData.ElementConnections.ContainsKey((el1, el2)) ? (el1, el2) :
                GameData.ElementConnections.ContainsKey((el2, el1)) ? (el2, el1) : null;
            if (pair == null) continue;

            double jAng = (angMap[el1].e + angMap[el2].s) * 0.5;
            string c1   = GameData.Elements[el1].Color;
            string c2   = GameData.Elements[el2].Color;
            string mc   = ColorHelper.Blend(c1, c2, 0.5);

            WedgeWF(0, 0, rIn, rOut, jAng - 4, jAng + 4, mc, 0.35);
            var (jl1x, jl1y) = Wpt(0, 0, rIn,  jAng);
            var (jl2x, jl2y) = Wpt(0, 0, rOut, jAng);
            LineW(jl1x, jl1y, jl2x, jl2y, PnB(border, mc, 0.60, 3));

            var (jx, jy) = Wpt(0, 0, rMid, jAng);
            int jseed = (int)((uint)(el1 + el2).GetHashCode() % (uint)GameData.RunesAll.Length);
            CircleW(jx, jy, 9, Br(mc, 0.45));
            RingWB(jx, jy, 9, mc, "#ffffff", 0.70, 2);
            TextWB(jx, jy, GameData.RunesAll[jseed].ToString(), mc, "#ffffff", 0.95, 8, isFixed: true);

            if (GameData.ElementConnections.TryGetValue(pair.Value, out string? conn))
            {
                string shortN = conn.Contains(" — ")
                    ? conn.Split(new[] { " — " }, StringSplitOptions.None)[0]
                    : conn.Length > 12 ? conn[..12] : conn;
                var (lx, ly) = Wpt(0, 0, rOut + 12, jAng);
                TextWB(lx, ly, shortN, mc, "#ffffff", 0.75, 5, isFixed: true);
            }

            if (GameData.SubelementNodes.TryGetValue(pair.Value, out var seNodes))
            {
                string keyStr = $"{pair.Value.Item1},{pair.Value.Item2}";
                s.SubelementNodes.TryGetValue(keyStr, out var bsub);
                if (bsub == null)
                    s.SubelementNodes.TryGetValue($"{pair.Value.Item2},{pair.Value.Item1}", out bsub);
                bsub ??= new();
                for (int k = 0; k < seNodes.Length; k++)
                {
                    var (snx, sny) = Wpt(0, 0, k % 2 == 0 ? rIn + 14 : rOut - 14, jAng + (k - 1) * 10.0);
                    bool bse = bsub.TryGetValue(seNodes[k].Name, out int bsc) && bsc > 0;
                    var capSeKey   = keyStr; var capSen = seNodes[k]; var capBsc = bsc;
                    string capSeDbKey  = $"subelemnode/{capSeKey}/{capSen.Name}";
                    bool   seDrawback  = bse && s.DrawbackBuys.ContainsKey(capSeDbKey);
                    DrawElemNode(snx, sny, capSen.Glyph, mc, 5, bse, seDrawback);
                    string pairLabel = $"{pair.Value.Item1} + {pair.Value.Item2}";
                    string connRight = GameData.ElementConnections.TryGetValue(pair.Value, out string? connDesc)
                        ? $"Connection: {pairLabel}\n{connDesc}" : $"Connection: {pairLabel}";
                    string seLeft = seDrawback
                        ? $"{capSen.Glyph} {capSen.Name}  ({capSen.Cost} pt)  ⚠ Drawback\n{pairLabel}  ·  Level: {capBsc}/3\n{capSen.Desc}\nRefunds −{capSen.Cost} pt  ·  Right-click to remove"
                        : bse
                            ? $"{capSen.Glyph} {capSen.Name}  ({capSen.Cost} pt)\n{pairLabel}  ·  Level: {capBsc}/3\n{capSen.Desc}\nRight-click: mark as drawback"
                            : $"{capSen.Glyph} {capSen.Name}  ({capSen.Cost} pt)\n{pairLabel}  ·  Level: {capBsc}/3\n{capSen.Desc}";
                    Action? seRC = bse
                        ? () => MutateSpell(sp => {
                            if (sp.DrawbackBuys.ContainsKey(capSeDbKey)) sp.DrawbackBuys.Remove(capSeDbKey);
                            else sp.DrawbackBuys[capSeDbKey] = capSen.Name;
                          })
                        : null;
                    Hit(snx, sny, 5,
                        seLeft,
                        connRight,
                        () => MutateSpell(sp => {
                            if (!sp.SubelementNodes.ContainsKey(capSeKey)) sp.SubelementNodes[capSeKey] = new();
                            int cur = sp.SubelementNodes[capSeKey].TryGetValue(capSen.Name, out int v) ? v : 0;
                            int nv  = cur >= 3 ? 0 : cur + 1;
                            sp.SubelementNodes[capSeKey][capSen.Name] = nv;
                            if (nv == 0) sp.DrawbackBuys.Remove(capSeDbKey);
                        }),
                        seRC);
                }
            }
        }
    }

    private void DrawElemNode(double wx, double wy, string symbol, string color, double r, bool bought, bool drawback = false)
    {
        if (drawback)
        {
            // Hollow white ring — drawback pip
            CircleW(wx, wy, r, Br(color, 0.10));
            RingWB(wx, wy, r, "#ffffff", "#aaaacc", 0.70, 2.5);
        }
        else
        {
            CircleW(wx, wy, r, Br(color, bought ? 0.55 : 0.18));
            RingW(wx, wy, r, bought ? color : ColorHelper.Blend(BgHex, color, 0.45), bought ? 2.0 : 1.0);
            if (bought) RingWF(wx, wy, r * 1.35, color, 0.25);
            TextW(wx, wy, symbol,
                  bought ? color : ColorHelper.Blend(BgHex, color, 0.55),
                  Math.Max(6, (int)(r * 0.9)));
        }
    }

    // ════════════════════════════════════════════════════════════
    //  LAYER 5 — SCHOOL MODULES
    // ════════════════════════════════════════════════════════════

    private void DrawSchoolModules(Dictionary<string, (double x, double y)> pos)
    {
        var s       = Spell!;
        var active  = new HashSet<string>(s.AllSchools);
        string primary = s.PrimarySchoolResolved;

        foreach (var (school, pt) in pos)
        {
            if (school == primary)
            {
                // Ghost outline at orbit so the user can see which slot it "belongs to"
                var sd = GameData.Schools[school];
                double ghostR = NodeRPri * s.CircleSizes.GetValueOrDefault(school, 1.0);
                RingWF(pt.x, pt.y, ghostR, sd.Color, 0.15, 1, true);
                TextWF(pt.x, pt.y, sd.Symbol, sd.Color, 0.22,
                       Math.Max(7, (int)(ghostR * 0.44)));
                // No hit at orbit for primary — interaction is at center
            }
            else
            {
                DrawModule(pt.x, pt.y, school, active, isPrimary: false);
            }
        }

        // Draw the designated primary school at the centre of the circle
        if (!string.IsNullOrEmpty(primary))
            DrawModule(0, 0, primary, active, isPrimary: true);
    }

    private void DrawModule(double wx, double wy, string school, HashSet<string> activeSet,
                            bool isPrimary = false)
    {
        var s     = Spell!;
        var sd    = GameData.Schools[school];
        string c  = sd.Color;
        bool act  = activeSet.Contains(school);
        double r  = isPrimary
            ? CenterR * 0.52
            : NodeRPri * s.CircleSizes.GetValueOrDefault(school, 1.0);
        bool cap  = s.CapstoneActive(school);
        double dim = act ? 1.0 : 0.20;
        string ink = "#c0cce0";  // cool silver for dark background

        int abBought = s.SchoolAbilities.TryGetValue(school, out var abD)
            ? abD.Values.Sum() : 0;
        int rmTotal  = s.RingMods.TryGetValue(school, out var rmD)
            ? rmD.Values.Sum() : 0;

        string statsRight = $"Circle Stats\nAbilities: {abBought}  ·  Ring mods: {rmTotal}/12\n" +
                            $"{(cap ? "⚜ Capstone Active!" : "Capstone: fill all 4 ring mods to 3")}";

        if (isPrimary)
        {
            // Primary school sits at centre: click to clear designation
            Hit(wx, wy, r,
                $"★ {sd.Symbol}  {school}  [PRIMARY]\n{sd.Desc}\nLeft-click: remove from centre",
                statsRight,
                () => MutateSpell(sp => sp.PrimarySchool = ""));

            // Prominent "primary" golden glow ring
            RingWF(wx, wy, r * 1.55, "#FFD700", 0.30, 2, true);
            RingW(wx, wy,  r * 1.42, "#FFD700", 1);
        }
        else
        {
            // Orbit school: left-click active school to designate as primary
            string clickHint = act ? "\nLeft-click: move to centre as Primary School" : "";
            Hit(wx, wy, r,
                $"◆ {sd.Symbol}  {school}\n{(act ? "Active" : "Inactive — buy abilities or ring mods to activate")}\n{sd.Desc}{clickHint}",
                statsRight,
                act ? () => MutateSpell(sp => sp.PrimarySchool = school) : null);
        }

        if (cap && act)
        {
            RingWF(wx, wy, r * 1.38, "#FFD700", 0.40, 1, true);
            RingW(wx, wy,  r * 1.30, "#FFD700", 2);
        }

        RingW(wx, wy, r,         act ? ColorHelper.Blend(ink, c, 0.35)
                                      : ColorHelper.Blend(BgHex, c, 0.22), act ? 2.0 : 1.0);
        RingWF(wx, wy, r * 0.92, c, 0.30 * dim);

        // Ability rune ring
        var abNames = sd.Abilities.Keys.ToList();
        s.SchoolAbilities.TryGetValue(school, out var abDict); abDict ??= new();
        int nAb = abNames.Count;
        for (int i = 0; i < Math.Min(nAb, 12); i++)
        {
            string abn = abNames[i];
            double a   = i * (360.0 / Math.Max(12, nAb));
            var (rx, ry) = Wpt(wx, wy, r * 0.84, a);
            int cnt  = abDict.TryGetValue(abn, out int cv) ? cv : 0;
            int seed = ((abn.Sum(ch => (int)ch) % GameData.RunesAll.Length)
                        + GameData.RunesAll.Length) % GameData.RunesAll.Length;
            string rune     = GameData.RunesAll[seed].ToString();
            var capAbSch    = school; var capAbn = abn;
            var abDef       = sd.Abilities[capAbn];
            string capAbKey = $"ability/{capAbSch}/{capAbn}";
            bool abDrawback = cnt > 0 && s.DrawbackBuys.ContainsKey(capAbKey);
            if (cnt > 0)
            {
                if (abDrawback)
                    RingWB(rx, ry, 3, "#ffffff", "#aaaacc", 0.70, 2.5);
                else
                {
                    CircleW(rx, ry, 3, Br(c, 0.65));
                    TextWB(rx, ry, rune, c, "#ffffff", 0.82, Math.Max(6, (int)(r * 0.20)), isFixed: true);
                }
            }
            else
            {
                CircleW(rx, ry, 2, Br(c, 0.28 * dim));
                TextWF(rx, ry, rune, c, 0.50 * dim, Math.Max(5, (int)(r * 0.16)), isFixed: true);
            }
            string abLeft = abDrawback
                ? $"⬥ {abn}  [{school}]  ⚠ Drawback\nCost: {abDef.Cost} pt  ·  Level: {cnt}/3\n{abDef.Desc}\nRefunds −{abDef.Cost} pt  ·  Right-click to remove"
                : cnt > 0
                    ? $"⬥ {abn}  [{school}]\nCost: {abDef.Cost} pt each  ·  Level: {cnt}/3\n{abDef.Desc}\nRight-click: mark as drawback (−{abDef.Cost} pt)"
                    : $"⬥ {abn}  [{school}]\nCost: {abDef.Cost} pt each  ·  Level: {cnt}/3\n{abDef.Desc}";
            Action? abRC = cnt > 0
                ? () => MutateSpell(sp => {
                    if (sp.DrawbackBuys.ContainsKey(capAbKey)) sp.DrawbackBuys.Remove(capAbKey);
                    else sp.DrawbackBuys[capAbKey] = capAbn;
                  })
                : null;
            Hit(rx, ry, Math.Max(r * 0.14, 6),
                abLeft,
                $"School: {sd.Symbol} {school}\n{sd.Desc}",
                () => MutateSpell(sp => {
                    if (!sp.SchoolAbilities.ContainsKey(capAbSch)) sp.SchoolAbilities[capAbSch] = new();
                    int cur = sp.SchoolAbilities[capAbSch].TryGetValue(capAbn, out int v) ? v : 0;
                    int nv  = cur >= 3 ? 0 : cur + 1;
                    sp.SchoolAbilities[capAbSch][capAbn] = nv;
                    if (nv == 0) sp.DrawbackBuys.Remove(capAbKey);
                }),
                abRC);
        }

        // Ring-mod rune ring
        string[] grpColors = ["#ff8080", "#80ee88", "#8088ff", "#ffe080"];
        s.RingMods.TryGetValue(school, out var ringData); ringData ??= new();
        for (int gi = 0; gi < GameData.RingGroups.Length; gi++)
        {
            string grp  = GameData.RingGroups[gi];
            string gc   = grpColors[gi];
            int fillCnt = ringData.TryGetValue(grp, out int fc) ? fc : 0;
            var runes   = GameData.ModRunes[grp];
            for (int slot = 0; slot < 3; slot++)
            {
                var (rx2, ry2) = Wpt(wx, wy, r * 0.64, (gi * 3 + slot) * 30.0);
                string rune    = runes[Math.Min(slot, runes.Length - 1)];
                bool   slotOn  = slot < fillCnt;
                var capRmSch   = school; var capGrp = grp; var capSlot = slot; var capFill = fillCnt;
                string capRmKey   = $"ringmod/{capRmSch}/{capGrp}/{capSlot}";
                bool   rmDrawback = slotOn && s.DrawbackBuys.ContainsKey(capRmKey);
                if (slotOn)
                {
                    if (rmDrawback)
                        RingWB(rx2, ry2, 3, "#ffffff", "#aaaacc", 0.70, 2.5);
                    else
                    {
                        CircleW(rx2, ry2, 3, Br(gc, 0.55));
                        TextWB(rx2, ry2, rune, gc, "#ffffff", 0.75, Math.Max(5, (int)(r * 0.20)), isFixed: true);
                    }
                }
                else
                {
                    CircleW(rx2, ry2, 2, Br(gc, 0.22 * dim));
                    TextWF(rx2, ry2, rune, gc, 0.42 * dim, Math.Max(5, (int)(r * 0.15)), isFixed: true);
                }
                string rmLabel2 = sd.RingMods.TryGetValue(grp, out var rl2) ? rl2 : grp;
                string rmLeft   = rmDrawback
                    ? $"◈ {rmLabel2}  [{school}]  Slot {slot + 1}/3  ⚠ Drawback\nCurrent: {fillCnt}  ·  Refunds −1 pt\nRight-click to remove drawback"
                    : slotOn
                        ? $"◈ {rmLabel2}  [{school}]  Slot {slot + 1}/3\nCurrent: {fillCnt}  ·  Click to set {(fillCnt == slot + 1 ? slot : slot + 1)}\nRight-click: mark as drawback (−1 pt)"
                        : $"◈ {rmLabel2}  [{school}]  Slot {slot + 1}/3\nCurrent: {fillCnt}  ·  Click to set {(fillCnt == slot + 1 ? slot : slot + 1)}";
                Action? rmRC = slotOn
                    ? () => MutateSpell(sp => {
                        if (sp.DrawbackBuys.ContainsKey(capRmKey)) sp.DrawbackBuys.Remove(capRmKey);
                        else sp.DrawbackBuys[capRmKey] = capRmKey;
                      })
                    : null;
                Hit(rx2, ry2, Math.Max(r * 0.14, 6),
                    rmLeft,
                    $"School: {sd.Symbol} {school}\n{sd.Desc}\n{(cap ? "⚜ Capstone Active!" : "Max all 4 ring mods at 3 for capstone")}",
                    () => MutateSpell(sp => {
                        if (!sp.RingMods.ContainsKey(capRmSch)) sp.RingMods[capRmSch] = new();
                        int nv = capFill == capSlot + 1 ? capSlot : capSlot + 1;
                        sp.RingMods[capRmSch][capGrp] = nv;
                        // Clean up orphaned ringmod drawbacks for slots now unfilled
                        for (int s2 = nv; s2 < 3; s2++)
                            sp.DrawbackBuys.Remove($"ringmod/{capRmSch}/{capGrp}/{s2}");
                    }),
                    rmRC);
            }
        }

        RingWF(wx, wy, r * 0.52, c, 0.30 * dim);
        RingWF(wx, wy, r * 0.35, c, 0.22 * dim);

        for (int i = 0; i < 8; i++)
        {
            var (x1, y1) = Wpt(wx, wy, r * 0.08, i * 45.0);
            var (x2, y2) = Wpt(wx, wy, r * 0.88, i * 45.0);
            LineWF(x1, y1, x2, y2, c, 0.18 * dim);
        }

        PolyNW(wx, wy, r * 0.32, 5,
               fill: Br(c, 0.08 * dim), stroke: PnF(c, 0.40 * dim, 1));

        TextW(wx, wy, sd.Symbol,
              act ? ColorHelper.Blend(c, "#ffffff", 0.55) : ColorHelper.Blend(BgHex, c, 0.30),
              Math.Max(8, (int)(r * 0.48)));

        if (cap && act && GameData.Capstones.TryGetValue(school, out var cd))
        {
            string capC = cd.Color;
            CircleW(wx, wy, r * 0.26, Br(capC, 0.28));
            TextW(wx, wy, cd.Glyph, capC, Math.Max(14, (int)(r * 0.58)));
            TextW(wx, wy + r * 1.52, $"⚜ {cd.Name} ⚜", capC, 5);
        }

        // Label: below the circle normally; above the hub ring when centred
        double labelY = isPrimary ? -(CenterR + 14) : wy + r + 10;
        double labelX = isPrimary ? 0               : wx;
        string labelC = isPrimary
            ? ColorHelper.Blend(c, "#FFD700", 0.55)
            : act ? ColorHelper.Blend(c, "#e8d8a0", 0.50)
                  : ColorHelper.Blend(BgHex, c, 0.22);
        TextW(labelX, labelY, isPrimary ? $"★ {school}" : school, labelC, 6);
    }

    // ════════════════════════════════════════════════════════════
    //  LAYER 6 — CENTER HUB
    // ════════════════════════════════════════════════════════════

    private void DrawCenterHub()
    {
        var s   = Spell!;
        double r    = CenterR;
        var cats    = GameData.CatColors.Keys.ToList();
        int nc      = cats.Count;
        double seg  = 360.0 / nc;

        // Determine whether a primary school is occupying the centre
        bool hasPrimary = !string.IsNullOrEmpty(s.PrimarySchoolResolved);

        if (!hasPrimary)
        {
            // ── Interior content (hidden when primary school is centred) ──
            CircleW(0, 0, r, Br(BgHex));               // void fill
            RingWF(0, 0, r * 0.42, "#5577bb", 0.22);

            for (int i = 0; i < nc; i++)
            {
                var (x1, y1) = Wpt(0, 0, r * 0.30, i * seg);
                var (x2, y2) = Wpt(0, 0, r * 0.92, i * seg);
                LineWF(x1, y1, x2, y2, "#aaaacc", 0.12);
            }
        }

        // ── Level info ───────────────────────────────────────────────────
        int pts    = s.TotalPoints;
        int lvlIdx = Math.Min(GameData.LevelTable.Count - 1,
                              GameData.LevelTable.TakeWhile(e => e.Hi < pts).Count());
        var lvl    = GameData.LevelTable[lvlIdx];
        double rGem = r * 0.28;

        if (!hasPrimary)
        {
            // Level gem drawn at the very centre
            CircleW(0, 0, rGem * 1.35, Br(lvl.Color, 0.25));
            RingWB(0, 0, rGem * 1.35, lvl.Color, "#ffffff", 0.40, 2);

            if      (lvlIdx <= 9)  TextW(0, 0, lvlIdx == 0 ? "C" : lvlIdx.ToString(), lvl.Color, Math.Max(18, (int)(rGem * 1.5)));
            else if (lvlIdx == 10) DrawFistW(0, 0, rGem, "#FFD700");
            else if (lvlIdx == 11) DrawCrownW(0, 0, rGem, "#FFD700");
            else if (lvlIdx == 12) DrawWingsW(0, 0, rGem, "#FFFFFF");
            else if (lvlIdx == 13) DrawSunW(0, 0, rGem, "#FFFFFF");
            else                   DrawStarsW(0, 0, rGem, "#FFFFFF");

            TextWB(0, rGem * 1.92, lvl.Name.ToUpper(), lvl.Color, "#aabbdd", 0.50, 5, isFixed: true);
        }
        else
        {
            // Level name sits just below the hub ring so it stays readable
            TextWB(0, r + 14, lvl.Name.ToUpper(), lvl.Color, "#aabbdd", 0.50, 5, isFixed: true);
        }

        // ── Hub outer rim — always drawn ─────────────────────────────────
        RingWB(0, 0, r, "#8899cc", "#ffffff", 0.55, 2);
        RingWF(0, 0, r * 0.94, "#5577bb", 0.22);

        for (int i = 0; i < 16; i++)
        {
            var (dx, dy) = Wpt(0, 0, r * 0.91, i * (360.0 / 16));
            CircleW(dx, dy, i % 4 == 0 ? 2.0 : 1.0, Br("#99aacc", 0.55));
        }

        for (int i = 0; i < cats.Count; i++)
        {
            string gc  = GameData.CatColors[cats[i]];
            double aS  = i * seg, midA = aS + seg / 2;
            ArcRingWF(0, 0, r, aS, seg - 3, gc, 0.50, 3);
            if (!hasPrimary)
            {
                // Category rune labels only when centre is empty
                var (lx, ly) = Wpt(0, 0, r * 0.87, midA);
                TextWF(lx, ly, GameData.ModRunes[cats[i]][0], gc, 0.65, 6, isFixed: true, angle: -(midA - 90));
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    //  MODIFIER RING  (middle ring at R×0.38)
    // ════════════════════════════════════════════════════════════

    private void DrawModRing(double R)
    {
        double orbit = R * 0.38;   // rides the faint inner geometry ring at this fraction
        double modCR = 22.0;       // world-space circle radius for each category circle

        var s    = Spell!;
        var cats = GameData.CatColors.Keys.ToList();
        int n    = cats.Count;
        double seg = 360.0 / n;

        // Faint dashed track ring — the circles sit on this
        RingWF(0, 0, orbit, "#aaaacc", 0.14, 1, true);

        for (int i = 0; i < n; i++)
        {
            string cat   = cats[i];
            double angle = i * seg + seg / 2;
            var (cx, cy) = Wpt(0, 0, orbit, angle);
            string gc    = GameData.CatColors[cat];
            var catMods  = GameData.DefaultGlobalMods.Where(kv => kv.Value.Cat == cat).ToList();
            bool hasAct  = catMods.Any(kv => s.GlobalMods.TryGetValue(kv.Key, out int v) && v > 0);

            CircleW(cx, cy, modCR, Br(gc, hasAct ? 0.13 : 0.05));
            RingW(cx, cy, modCR, ColorHelper.Blend(BgHex, gc, hasAct ? 0.65 : 0.30), hasAct ? 2 : 1);
            RingWF(cx, cy, modCR * 0.68, gc, 0.18);

            for (int k = 0; k < 4; k++)
            {
                var (p1x, p1y) = Wpt(cx, cy, modCR * 0.12, angle + k * 90);
                var (p2x, p2y) = Wpt(cx, cy, modCR * 0.62, angle + k * 90);
                LineWF(p1x, p1y, p2x, p2y, gc, 0.18);
            }

            string baseRune = GameData.ModRunes[cat][0];
            TextW(cx, cy, baseRune,
                  hasAct ? ColorHelper.Blend(gc, "#ffffff", 0.75) : ColorHelper.Blend(BgHex, gc, 0.45),
                  Math.Max(6, (int)(modCR * 0.50)), isFixed: true);

            Hit(cx, cy, modCR,
                $"■ {cat} Modifiers\n" +
                string.Join("\n", catMods.Select(kv =>
                    $"  {kv.Key} ({kv.Value.Cost} pt, max {kv.Value.Max})")),
                $"Spell Overview\nLevel: {s.LevelName}\nTotal pts: {s.TotalPoints}\nSchools: {string.Join(", ", s.AllSchools)}");

            int nm = catMods.Count;
            if (nm == 0) continue;
            double nodeR = modCR * 0.82;
            for (int j = 0; j < nm; j++)
            {
                var (rx, ry) = Wpt(cx, cy, nodeR, j * (360.0 / nm));
                int cnt  = s.GlobalMods.TryGetValue(catMods[j].Key, out int v2) ? v2 : 0;
                int seed = ((catMods[j].Key.Sum(ch => (int)ch) % GameData.RunesAll.Length)
                            + GameData.RunesAll.Length) % GameData.RunesAll.Length;
                string rune      = GameData.RunesAll[seed].ToString();
                var capModKey    = catMods[j].Key; var capModDef = catMods[j].Value; var capModCnt = cnt;
                string capModDbKey  = $"mod/{capModKey}";
                bool   modDrawback  = cnt > 0 && s.DrawbackBuys.ContainsKey(capModDbKey);
                if (cnt > 0)
                {
                    if (modDrawback)
                        RingWB(rx, ry, 3, "#ffffff", "#aaaacc", 0.70, 2.5);
                    else
                    {
                        CircleW(rx, ry, 3, Br(gc, 0.65));
                        TextWB(rx, ry, rune, gc, "#ffffff", 0.80, Math.Max(5, (int)(modCR * 0.30)), isFixed: true);
                    }
                }
                else
                {
                    CircleW(rx, ry, 2, Br(gc, 0.14));
                    TextWF(rx, ry, rune, gc, 0.35, Math.Max(5, (int)(modCR * 0.26)), isFixed: true);
                }
                string modLeft = modDrawback
                    ? $"◈ {capModKey}  [{cat}]  ⚠ Drawback\nCost: {capModDef.Cost} pt  ·  Owned: {capModCnt}/{capModDef.Max}\n{capModDef.Desc}\nRefunds −{capModDef.Cost} pt  ·  Right-click to remove"
                    : cnt > 0
                        ? $"◈ {capModKey}  [{cat}]\nCost: {capModDef.Cost} pt  ·  Owned: {capModCnt}/{capModDef.Max}\n{capModDef.Desc}\nRight-click: mark as drawback (−{capModDef.Cost} pt)"
                        : $"◈ {capModKey}  [{cat}]\nCost: {capModDef.Cost} pt  ·  Owned: {capModCnt}/{capModDef.Max}\n{capModDef.Desc}";
                Action? modRC = cnt > 0
                    ? () => MutateSpell(sp => {
                        if (sp.DrawbackBuys.ContainsKey(capModDbKey)) sp.DrawbackBuys.Remove(capModDbKey);
                        else sp.DrawbackBuys[capModDbKey] = capModKey;
                      })
                    : null;
                Hit(rx, ry, 7,
                    modLeft,
                    $"Category: {cat}\nLevel: {s.LevelName}\nTotal pts: {s.TotalPoints}",
                    () => MutateSpell(sp => {
                        int cur = sp.GlobalMods.TryGetValue(capModKey, out int v) ? v : 0;
                        int nv  = cur >= capModDef.Max ? 0 : cur + 1;
                        sp.GlobalMods[capModKey] = nv;
                        if (nv == 0) sp.DrawbackBuys.Remove(capModDbKey);
                    }),
                    modRC);
            }
        }
    }

    // ── Level icons ───────────────────────────────────────────────
    private void DrawFistW(double wx, double wy, double r, string color)
    {
        double p0 = r * 0.18;
        PolyWF(new[] { (wx-r*.55,wy+p0),(wx+r*.55,wy+p0),(wx+r*.55,wy+r*.65),(wx-r*.55,wy+r*.65) }, color, 0.75);
        for (int i = 0; i < 4; i++)
            CircleW(wx + (-r * 0.42 + i * r * 0.28), wy - r * 0.02, r * 0.20, Br(color, 0.80));
    }
    private void DrawCrownW(double wx, double wy, double r, string color)
    {
        double by = r * 0.12;
        PolyWF(new[] { (wx-r*.72,wy+by),(wx+r*.72,wy+by),(wx+r*.72,wy+r*.55),(wx-r*.72,wy+r*.55) }, color, 0.70);
        foreach (double px in new[] { -0.68, -0.34, 0.0, 0.34, 0.68 })
            PolyWF(new[] { (wx+px*r-r*.16,wy+by),(wx+px*r,wy-r*.52),(wx+px*r+r*.16,wy+by) }, color, 0.90);
    }
    private void DrawWingsW(double wx, double wy, double r, string color)
    {
        foreach (int side in new[] { -1, 1 })
        {
            var pts = new List<(double, double)> { (wx, wy) };
            for (int i = 0; i <= 9; i++)
            {
                double t = i / 9.0;
                pts.Add(Wpt(wx, wy, r * (0.25 + t * 0.80), 180 + side * (20 + t * 110)));
            }
            pts.Add((wx, wy));
            PolyW(pts, Br(color, 0.25));
        }
    }
    private void DrawSunW(double wx, double wy, double r, string color)
    {
        CircleW(wx, wy, r * 0.32, Br(color, 0.70));
        for (int i = 0; i < 12; i++)
        {
            var (x1, y1) = Wpt(wx, wy, r * 0.42, i * 30.0);
            var (x2, y2) = Wpt(wx, wy, r * 0.88, i * 30.0);
            LineW(x1, y1, x2, y2, color, 2);
        }
    }
    private void DrawStarsW(double wx, double wy, double r, string color)
    {
        for (int i = 0; i < 7; i++)
        {
            var (sx, sy) = Wpt(wx, wy, r * 0.62, i * (360.0 / 7));
            StarW(sx, sy, r * 0.25, r * 0.12, 5, fill: Br(color, 0.85));
        }
    }

    // ════════════════════════════════════════════════════════════
    //  LAYER 7 — DRAWBACK RINGS
    // ════════════════════════════════════════════════════════════

    private void DrawDrawbackRings()
    {
        var s = Spell!;

        // Collect active "neg/" drawbacks in order
        var active = s.DrawbackBuys.Keys
            .Where(k => k.StartsWith("neg/"))
            .Select(k => k[4..])
            .ToList();

        if (active.Count == 0) return;

        int    n     = active.Count;
        double orbit = CenterR + 22;
        double ringR = 8;

        // Faint orbit ring
        RingWF(0, 0, orbit, "#882222", 0.30, 1, true);

        for (int i = 0; i < n; i++)
        {
            string name  = active[i];
            double angle = i * (360.0 / n);
            var (rx, ry) = Wpt(0, 0, orbit, angle);

            // Look up cost
            var def  = GameData.DefaultNegativeMods.FirstOrDefault(m => m.Name == name);
            int cost = def?.Cost ?? s.CustomNegMods.FirstOrDefault(m => m.Name == name)?.Cost ?? 2;
            string costHex = cost >= 3 ? "#ff4444" : cost == 2 ? "#dd6644" : "#cc8844";

            // Token: filled crimson circle
            CircleW(rx, ry, ringR, Br(costHex, 0.25));
            RingWB(rx, ry, ringR, costHex, "#ffffff", 0.55, 1.5);

            // Initial letter label
            string lbl = name.Length > 0 ? name[0].ToString().ToUpper() : "?";
            TextWB(rx, ry, lbl, costHex, "#ffffff", 0.80, Math.Max(5, (int)(ringR * 0.80)), isFixed: true);

            // Hit region — hover shows full drawback info; right-click removes
            var capName = name; var capCost = cost; var capDesc = def?.Desc ?? "";
            string capNegKey = $"neg/{capName}";
            Hit(rx, ry, ringR,
                $"✕ {capName}  (−{capCost} pt)\n{capDesc}\nRight-click to remove drawback",
                $"Drawbacks Active: {n}\nTotal Refund: −{s.DrawbackRefund} pts\n" +
                $"Rule: refund capped at 50% of gross cost",
                null,
                () => MutateSpell(sp => sp.DrawbackBuys.Remove(capNegKey)));
        }
    }

    // ════════════════════════════════════════════════════════════
    //  LAYER 8 — CONDITIONS RING
    // ════════════════════════════════════════════════════════════

    private void DrawConditionsRing()
    {
        var s = Spell!;
        var all = s.IfThenConditions.Select(c => ("if", c))
                   .Concat(s.WhenThenConditions.Select(c => ("wt", c))).ToList();
        if (all.Count == 0) return;

        double orbit = CenterR + 50;
        int n        = all.Count;
        RingWF(0, 0, orbit, "#8899bb", 0.22, 1, true);

        string[] ifP = ["#6080ff","#8899ff","#aabbff","#99aaee","#7788dd",
                         "#5566cc","#8899ee","#aaccff","#6677bb","#9999dd"];
        string[] wtP = ["#ff9960","#60dd88","#ffdd60","#ff88ff","#88ffff",
                         "#ff6080","#88ddff","#ddff88","#ffaa88","#aaffaa"];

        for (int i = 0; i < n; i++)
        {
            var (kind, cond) = all[i];
            double a  = i * (360.0 / n);
            var (rx, ry) = Wpt(0, 0, orbit, a);
            string c  = kind == "if" ? ifP[i % ifP.Length] : wtP[i % wtP.Length];
            string l1 = cond.IfOrWhenText, l2 = cond.ThenText;
            int seed  = (((l1 + l2).Sum(ch => (int)ch) + i * 37) % GameData.RunesAll.Length
                         + GameData.RunesAll.Length) % GameData.RunesAll.Length;
            string rune = GameData.RunesAll[seed].ToString();

            if (kind == "if")
            {
                double d = 8;
                PolyW(new[] { (rx,ry-d),(rx+d,ry),(rx,ry+d),(rx-d,ry) },
                      Br(c, 0.30), Pn(B(c, "#ffffff", 0.60), 1));
            }
            else
            {
                CircleW(rx, ry, 7, Br(c, 0.28));
                RingWB(rx, ry, 7, c, "#ffffff", 0.60);
            }
            TextWB(rx, ry, rune, c, "#ffffff", 0.88, 8, isFixed: true);
        }
    }

    // ════════════════════════════════════════════════════════════
    //  STATUS BAR
    // ════════════════════════════════════════════════════════════

    private void DrawStatusBar()
    {
        var s   = Spell!;
        var lvl = s.LevelInfo;
        string col = ColorHelper.Blend("#445566", lvl.Color, 0.7);
        // Include attrition type when set
        var attrDef = GameData.AttritionTypes.FirstOrDefault(a => a.Name == s.AttritionType);
        string attrPart = (attrDef != null && attrDef.Name != "None")
            ? $"  ·  {attrDef.Symbol} {attrDef.Name} Attrition"
            : "";
        string txt = $"{lvl.Name}  ·  {s.TotalPoints} pts{attrPart}  ·  {_zoom:F2}×  [scroll=zoom  R-drag=pan]";
        var ft  = new FormattedText(txt, CultureInfo.CurrentCulture,
                                    FlowDirection.LeftToRight, _tfGeo, 9 * _zoom, Br(col), 1.0);
        var cp  = Tc(0, OuterR + 14);
        _dc.DrawText(ft, new Point(cp.X - ft.Width / 2, cp.Y - ft.Height / 2));

        if (s.DrawbackBuys.Count > 0 && !s.IsComplete)
        {
            var ft2 = new FormattedText("⚠ more drawbacks than purchases — add normal items",
                                        CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                                        _tf, 7 * _zoom, Br("#ff5555"), 1.0);
            var cp2 = Tc(0, OuterR + 26);
            _dc.DrawText(ft2, new Point(cp2.X - ft2.Width / 2, cp2.Y - ft2.Height / 2));
        }
    }
}
