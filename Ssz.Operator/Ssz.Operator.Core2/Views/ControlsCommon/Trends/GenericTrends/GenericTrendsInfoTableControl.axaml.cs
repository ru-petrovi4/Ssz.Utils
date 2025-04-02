using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Egorozh.ColorPicker.Dialog;
using Binding = Avalonia.Data.Binding;
using Button = Avalonia.Controls.Button;
using CheckBox = Avalonia.Controls.CheckBox;
using UserControl = Avalonia.Controls.UserControl;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends;

public partial class GenericTrendsInfoTableControl : UserControl
{
    #region construction and destruction

    public GenericTrendsInfoTableControl()
    {
        InitializeComponent();
    }

    #endregion    

    #region private functions    

    private async void ChooseColorButton_OnClick(object? sender, RoutedEventArgs args)
    {
        var button = (Button)sender!;

        ColorPickerDialog dialog = new()
        {
            //Color = Color,
            //Colors = Colors,
            //Title = "Custom Title"
        };

        var result = await dialog.ShowDialog<bool>((Window)TopLevel.GetTopLevel(this)!);

        if (result)
        {
            var color = dialog.Color;
            button.Background = new SolidColorBrush(color);
        }        
    }

    #endregion
}