using Avalonia;
using Avalonia.Controls;

namespace Ssz.Operator.Core;

public partial class DebugWindow : Window
{
    #region construction and destruction

    public DebugWindow()
    {
        InitializeComponent();
    }

    #endregion

    #region public functions

    /// <summary>
    ///     Created if needed
    /// </summary>
    public static DebugWindow Instance
    {
        get
        {
            if (_instance is null)
            {
                _instance = new DebugWindow();
                try
                {
                    _instance.Owner = MessageBoxHelper.GetRootWindow();
                }
                catch
                {
                }
                _instance.Closed += (sender, args) => { _instance = null; };
                _instance.Show();
            }

            return _instance;
        }
    }

    public static bool IsWindowExists => _instance is not null;

    public void Clear()
    {
        MainStackPanel.Children.Clear();
    }

    public void AddLine(string line)
    {
        MainStackPanel.Children.Add(
            new TextBox { BorderThickness = new Thickness(0), IsReadOnly = true, Text = line });

        MainScrollViewer.ScrollToEnd();
    }

    #endregion

    #region private fields

    private static DebugWindow? _instance;

    #endregion
}