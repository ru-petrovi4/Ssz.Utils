using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestWpfApp
{
    /// <summary>
    /// Interaction logic for DrawControl.xaml
    /// </summary>
    public partial class DrawControl : UserControl
    {
        public DrawControl()
        {
            InitializeComponent();
        }

        protected override void OnRender(DrawingContext dc)
        {
            var bytes0 = System.Convert.FromBase64String(_s0);
            var bytes1 = System.Convert.FromBase64String(_s1);
            var bytes2 = System.Convert.FromBase64String(_s2);

            DrawBytes(dc, bytes0, 0, 0);            
            DrawBytes(dc, bytes1, 0, 10);            
            DrawBytes(dc, bytes2, 0, 20);            

            base.OnRender(dc);
        }

        private void DrawBytes(DrawingContext dc, byte[] bytes, int offset, double y)
        {
            for (int i = 0; i < 32; i++)
            {
                byte b = (byte)(bytes[i + offset]); //  (bytes[i] > 127) ? (byte)0 : (byte)255;
                dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(b, b, b)), null, new RectangleGeometry(new Rect(i * 10, y, 10, 10)));
            }
        }

        string _s0 = "+WOBA94T4gOaA9zo+Ygda+9GcZecFC1fGy1TdfXCpRw=";
        string _s1 = "R7/hRrRroSQ+MnWAhoLl2THHTWfZfmXsq3+PuVTRL6k=";
        string _s2 = "m9zyvlcodIYY+B1j2FK21mmvchyFfylNjO/jjtTU9Cg=";

        
    }
}


//DrawBytes(dc, bytes0, 0, 0);
//DrawBytes(dc, bytes0, 16, 10);
//DrawBytes(dc, bytes1, 0, 20);
//DrawBytes(dc, bytes1, 16, 30);
//DrawBytes(dc, bytes2, 0, 40);
//DrawBytes(dc, bytes2, 16, 50);

//dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(bytes[4 * i], bytes[4 * i + 1], bytes[4 * i + 2], bytes[4 * i + 3])), null, new Rect(i * 10, y, 10, 10));

//byte b = bytes[i];

//dc.DrawGeometry(null, new Pen(new SolidColorBrush(Color.FromRgb(b, b, b)), 7), new RectangleGeometry(new Rect(i * 10, y, 10, 10)));

//private void DrawBytes(DrawingContext dc, byte[] bytes)
//{
//    for (int i = 0; i < bytes.Length / 2 - 1; i++)
//    {
//        DrawTick(dc, bytes[2 * i], bytes[2 * i + 1], bytes[2 * i + 2], bytes[2 * i + 3]);
//    }
//}

//private void DrawTick(DrawingContext dc, double x1, double y1, double x2, double y2)
//{
//    dc.DrawGeometry(null, new Pen(Brushes.Black, 2), new RectangleGeometry(new Rect(x1, y1, x2, y2)));
//}


//string _s0 = "+WOBA94T4gOaA9zo+Ygda+9GcZecFC1fGy1TdfXCpRw=";
//string _s1 = "R7/hRrRroSQ+MnWAhoLl2THHTWfZfmXsq3+PuVTRL6k=";
//string _s2 = "m9zyvlcodIYY+B1j2FK21mmvchyFfylNjO/jjtTU9Cg=";


//string _s0 = "+WOBA94T4gOaA9zo+Ygda+=";
//string _s1 = "9GcZecFC1fGy1TdfXCpRw=";
//string _s2 = "R7/hRrRroSQ+MnWAhoLl2T=";
//string _s3 = "HHTWfZfmXsq3+PuVTRL6k=";
//string _s4 = "m9zyvlcodIYY+B1j2FK21m=";
//string _s5 = "mvchyFfylNjO/jjtTU9Cg=";