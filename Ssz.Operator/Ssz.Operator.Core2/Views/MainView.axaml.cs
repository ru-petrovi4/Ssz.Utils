using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client;
using Ssz.Operator.Core;
using Ssz.Operator.Play.ViewModels;
using Ssz.Utils.Logging;
using System;
using System.Threading.Tasks;

namespace Ssz.Operator.Play.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        Logger = new UserFriendlyLogger<GrpcDataAccessProvider>((l, e, v) =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainTextBlock.Text += $"\nLogger: {v}";
            });
        });
    }

    public ILogger<GrpcDataAccessProvider> Logger { get; private set; } = null!;    

    private void Button_OnClick(object? sender, RoutedEventArgs args)
    {
        var t = DoAsync();
    }

    private async Task DoAsync()
    {
        JobViewModel jobViewModel = new(@"", @"", @"");
        MainProgressBar.DataContext = jobViewModel;
        try
        {
            if (OperatingSystem.IsBrowser())
            {
                await ((App)Application.Current!).OnFrameworkInitializationCompleted2(jobViewModel);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Exception: {ex}");
        }

        Logger.LogInformation("DoAsync end.");
    }
}