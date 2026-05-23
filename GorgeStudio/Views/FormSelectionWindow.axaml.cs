using System.Threading.Tasks;
using Avalonia.Controls;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public partial class FormSelectionWindow : Window
{
    public FormSelectionWindow()
    {
        InitializeComponent();
    }

    public static async Task<bool> ShowAsync(Window owner, FormSelectionWindowViewModel viewModel)
    {
        var window = new FormSelectionWindow { DataContext = viewModel };
        viewModel.CloseAction = () => window.Close();
        await window.ShowDialog(owner);
        return viewModel.DialogResult;
    }
}
