using Avalonia.Threading;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Utils
{
    public static class DispatcherHelper
    {
        public static Ssz.Utils.IDispatcher GetUiDispatcher()
        {
            if (OperatingSystem.IsBrowser())
                return new DummyDispatcher();
            else
                return new WrapperDispatcher(Dispatcher.UIThread);
        }
    }
}
