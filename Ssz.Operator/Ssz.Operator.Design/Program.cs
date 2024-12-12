using System;

namespace Ssz.Operator.Design
{
    public static class Program
    {
        #region public functions

        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        #endregion
    }
}