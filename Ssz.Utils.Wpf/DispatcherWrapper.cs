using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Ssz.Utils.Wpf
{   
    public class DispatcherWrapper : IDispatcher
    {
        #region construction and destruction

        public DispatcherWrapper(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        #endregion

        #region public functions

        public void BeginInvoke(Action<CancellationToken> action)
        {
            _dispatcher.BeginInvoke(action);
        }

        #endregion

        #region private fields

        private Dispatcher _dispatcher;

        #endregion
    }
}
