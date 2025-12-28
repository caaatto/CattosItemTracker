using Avalonia.Controls;
using Avalonia.Interactivity;
using CattosTracker.ViewModels;

namespace CattosTracker.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Pass window reference to ViewModel for file dialogs
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetMainWindow(this);
        }
    }

    private void SetAsMainButton_Click(object? sender, RoutedEventArgs e)
    {
        // Get the ViewModel and call the SetAsMain method
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetAsMain();
        }
    }

    private void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        // Manual refresh
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ManualRefresh();
        }
    }

    private void SyncButton_Click(object? sender, RoutedEventArgs e)
    {
        // Force sync to API
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ForceSync();
        }
    }
}
