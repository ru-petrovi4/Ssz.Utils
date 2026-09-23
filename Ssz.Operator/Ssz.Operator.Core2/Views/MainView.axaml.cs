using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.Client;
using Ssz.Operator.Core;
using Ssz.Operator.Core.ViewModels;
using Ssz.Utils.Logging;
using System;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Views;

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
}