using System.Threading.Tasks;
using Avalonia.Controls;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public partial class ProjectSettingsWindow : Window
{
    public ProjectSettingsWindow()
    {
        InitializeComponent();
    }

    public static async Task<bool> ShowAsync(Window owner, ProjectSettingsWindowViewModel viewModel)
    {
        var window = new ProjectSettingsWindow { DataContext = viewModel };
        viewModel.CloseAction = () => window.Close();
        await window.ShowDialog(owner);
        return viewModel.DialogResult;
    }
}
