using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
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
        foreach (var schoolVm in vm.SchoolViewModels)
        {
            var tab = new TabItem
            {
                Header  = schoolVm.Name,
                Content = new SchoolPanel { DataContext = schoolVm },
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
