using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace SpellForge.Views.Controls;

public partial class GlobalModPanel : UserControl
{
    public static readonly DependencyProperty CategoriesProperty =
        DependencyProperty.Register(
            nameof(Categories),
            typeof(IEnumerable),
            typeof(GlobalModPanel),
            new PropertyMetadata(null));

    public IEnumerable Categories
    {
        get => (IEnumerable)GetValue(CategoriesProperty);
        set => SetValue(CategoriesProperty, value);
    }

    public GlobalModPanel()
    {
        InitializeComponent();
    }
}
