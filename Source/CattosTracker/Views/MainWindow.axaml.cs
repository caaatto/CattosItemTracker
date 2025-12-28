using System;
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

    private async void SelectWowPath_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Open folder picker for WoW path directly from here
            var storage = this.StorageProvider;
            var result = await storage.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
            {
                Title = "Wähle dein World of Warcraft Verzeichnis",
                AllowMultiple = false
            });

            if (result != null && result.Count > 0 && DataContext is MainWindowViewModel viewModel)
            {
                var selectedPath = result[0].Path.LocalPath;
                await viewModel.ProcessSelectedWowPath(selectedPath);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SelectWowPath_Click] Error: {ex.Message}");
        }
    }
}
