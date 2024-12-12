using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Ssz.Operator.Core.VisualEditors.ValueConverters;
using Ssz.Operator.Core.VisualEditors.Windows;

namespace Ssz.Operator.Core.VisualEditors
{
    public partial class XamlEditor : UserControl
    {
        #region construction and destruction

        public XamlEditor()
        {
            InitializeComponent();

            MainButton.SetBinding(ContentProperty, new Binding
            {
                Path = new PropertyPath(@"ConstXaml"),
                Converter =
                    XamlToContentConverter
                        .Instance
            });
        }

        #endregion

        #region protected functions

        protected virtual void ButtonClick(object? sender, RoutedEventArgs e)
        {
            string? xaml = null;
            if (((StatementViewModel) DataContext).ConstXaml is not null)
                xaml = ((StatementViewModel) DataContext).ConstXaml?.Xaml;
            var dialog = new ConstContentEditorDialog
            {
                Xaml = xaml ?? "",
                Owner = Window.GetWindow(this)
            };

            if (dialog.ShowDialog() == true)
                ((StatementViewModel) DataContext).ConstXaml = new DsXaml {Xaml = dialog.Xaml};
        }

        #endregion
    }
}