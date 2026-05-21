using System.Windows;
using System.Windows.Controls;
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
}
