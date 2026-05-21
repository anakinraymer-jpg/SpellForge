using System.Windows;
using SpellForge.ViewModels;

namespace SpellForge;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new SpellViewModel();
        DataContext = vm;
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();
}
