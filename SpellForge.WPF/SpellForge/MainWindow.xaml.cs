using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using SpellForge.Models;
using SpellForge.ViewModels;
using SpellForge.Views.Controls;

namespace SpellForge;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new SpellViewModel();
        DataContext = vm;
        Loaded += (_, _) => PopulateSchoolTabs(vm);
    }

    private void PopulateSchoolTabs(SpellViewModel vm)
    {
        var panelBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0d0d1a"));

        foreach (var (domainName, schools) in GameData.SchoolDomains)
        {
            var di          = GameData.DomainInfo[domainName];
            var domainColor = (Color)ColorConverter.ConvertFromString(di.Color);

            // ── Domain tab header: symbol + name in domain colour ──
            var domainHeader = new TextBlock
            {
                Text       = $"{di.Symbol}  {domainName}",
                Foreground = new SolidColorBrush(domainColor),
                FontSize   = 11,
                Padding    = new Thickness(0, 1, 0, 1),
            };

            // ── One Expander per school inside the domain ──────────
            var stack = new StackPanel { Margin = new Thickness(2) };
            foreach (var schoolName in schools)
            {
                var schoolVm    = vm.SchoolViewModels.First(s => s.Name == schoolName);
                var schoolColor = (Color)ColorConverter.ConvertFromString(schoolVm.Color);
                var dimColor    = Color.FromArgb(0x44, schoolColor.R, schoolColor.G, schoolColor.B);

                var expHeader = new StackPanel { Orientation = Orientation.Horizontal };
                expHeader.Children.Add(new TextBlock
                {
                    Text       = schoolVm.Symbol,
                    Foreground = new SolidColorBrush(schoolColor),
                    FontSize   = 13,
                    Width      = 22,
                    TextAlignment = System.Windows.TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                });
                expHeader.Children.Add(new TextBlock
                {
                    Text       = schoolName,
                    Foreground = new SolidColorBrush(schoolColor),
                    FontSize   = 11,
                    VerticalAlignment = VerticalAlignment.Center,
                });

                var expander = new Expander
                {
                    Header          = expHeader,
                    Content         = new SchoolPanel { DataContext = schoolVm },
                    IsExpanded      = false,
                    Background      = panelBg,
                    BorderBrush     = new SolidColorBrush(dimColor),
                    BorderThickness = new Thickness(0, 0, 0, 1),
                    Margin          = new Thickness(0, 0, 0, 1),
                };
                stack.Children.Add(expander);
            }

            var tab = new TabItem
            {
                Header  = domainHeader,
                Content = new ScrollViewer
                {
                    Content                       = stack,
                    VerticalScrollBarVisibility   = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    Background                    = panelBg,
                },
            };
            LeftTabs.Items.Add(tab);
        }

        ModPanel.Categories = vm.GlobalModCategories;
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    // ── Canvas export ─────────────────────────────────────────────
    private RenderTargetBitmap RenderCanvas()
    {
        // Render at 2× screen resolution for a crisp PNG
        const double dpi   = 192.0;
        const double scale = dpi / 96.0;
        var w = (int)Math.Max(1, MagicCanvas.ActualWidth  * scale);
        var h = (int)Math.Max(1, MagicCanvas.ActualHeight * scale);

        var rtb = new RenderTargetBitmap(w, h, dpi, dpi, PixelFormats.Pbgra32);

        // Use a DrawingVisual + VisualBrush so the canvas renders at the
        // scaled resolution rather than just being pixel-stretched.
        var dv = new DrawingVisual();
        using (var ctx = dv.RenderOpen())
        {
            var vb = new VisualBrush(MagicCanvas);
            ctx.DrawRectangle(vb, null, new Rect(0, 0, w, h));
        }
        rtb.Render(dv);
        return rtb;
    }

    private void MenuSavePng_Click(object sender, RoutedEventArgs e)
    {
        var vm       = DataContext as SpellViewModel;
        var safeName = (vm?.Spell.Name ?? "spell")
                       .Replace(" ", "_")
                       .Replace("/", "-")
                       .Replace("\\", "-");

        var dlg = new SaveFileDialog
        {
            Filter     = "PNG Image (*.png)|*.png",
            DefaultExt = "png",
            FileName   = safeName,
        };
        if (dlg.ShowDialog() != true) return;

        var enc = new PngBitmapEncoder();
        enc.Frames.Add(BitmapFrame.Create(RenderCanvas()));
        using var stream = File.Create(dlg.FileName);
        enc.Save(stream);

        if (vm != null) vm.StatusText = $"PNG saved: {dlg.FileName}";
    }

    private void MenuCopyCanvas_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetImage(RenderCanvas());
        if (DataContext is SpellViewModel vm)
            vm.StatusText = "Canvas copied to clipboard.";
    }
}
