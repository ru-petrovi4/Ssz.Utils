using Ssz.Operator.Core.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public class BrowserPlayWindowViewModel : DataValueViewModel
    {
        public BrowserPlayWindowViewModel(IPlayWindowBase? playWindow, bool visualDesignMode) : 
            base(playWindow, visualDesignMode)
        {
        }

        public bool IsNotRootWindow { get; set; }
    }
}
