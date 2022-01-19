//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using System.Windows.Input;

//namespace Ssz.Utils.Wpf
//{
//    public class ControlDoubleClick : DependencyObject
//    {
//        public static readonly DependencyProperty CommandProperty =
//            DependencyProperty.RegisterAttached("Command", typeof(ICommand), typeof(ControlDoubleClick), new PropertyMetadata(OnChangedCommand));

//        public static ICommand GetCommand(Control target)
//        {
//            return (ICommand)target.GetValue(CommandProperty);
//        }

//        public static void SetCommand(Control target, ICommand value)
//        {
//            target.SetValue(CommandProperty, value);
//        }

//        private static void OnChangedCommand(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            Control control = (Control)d;
//            control.DoubleTapped += ElementOnPreviewMouseDoubleClick;
//        }

//        private static void ElementOnPreviewMouseDoubleClick(object? sender, MouseButtonEventArgs e)
//        {
//            Control control = (Control)sender!;
//            ICommand command = GetCommand(control);

//            if (command.CanExecute(null))
//            {
//                command.Execute(null);
//                e.Handled = true;
//            }
//        }
//    }
//}
