using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using GorgeStudio.ViewModels;

namespace GorgeStudio.Views;

public partial class RenameStaffDisplayNameWindow : Window
{
    public RenameStaffDisplayNameWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(System.EventArgs e)
    {
        base.OnOpened(e);
        // Focus and select all text in the DisplayName TextBox
        Dispatcher.UIThread.Post(() =>
        {
            DisplayNameTextBox.Focus();
            DisplayNameTextBox.SelectAll();
        }, DispatcherPriority.Loaded);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (DataContext is not RenameStaffDisplayNameWindowViewModel vm) return;

        if (e.Key == Key.Enter)
        {
            vm.OkCommand.Execute(null);
        }
        else if (e.Key == Key.Escape)
        {
            vm.CancelCommand.Execute(null);
        }
    }

    public static async Task<bool> ShowAsync(Window owner, RenameStaffDisplayNameWindowViewModel viewModel)
    {
        var window = new RenameStaffDisplayNameWindow { DataContext = viewModel };
        viewModel.CloseAction = () => window.Close();
        await window.ShowDialog(owner);
        return viewModel.DialogResult;
    }
}
