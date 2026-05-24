using System.Threading.Tasks;
using Avalonia.Controls;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public partial class StaffTypeSelectionWindow : Window
{
    public StaffTypeSelectionWindow()
    {
        InitializeComponent();
    }

    public static async Task<bool> ShowAsync(Window owner, StaffTypeSelectionWindowViewModel viewModel)
    {
        var window = new StaffTypeSelectionWindow { DataContext = viewModel };
        viewModel.CloseAction = () => window.Close();
        await window.ShowDialog(owner);
        return viewModel.DialogResult;
    }
}
