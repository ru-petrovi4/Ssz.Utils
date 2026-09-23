using Avalonia;
using Avalonia.Browser;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using static Ssz.Operator.Core.DsProject;

internal sealed partial class Program
{
    private static Task Main(string[] args)
    {
        AddonsManager.AddonsSearchPattern = @"Ssz.Operator.Addons.*.dll";

        return BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<PlayApp>();
}