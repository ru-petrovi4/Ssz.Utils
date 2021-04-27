using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ssz.Xceed.Wpf.Toolkit.Zoombox
{
    public class ResolveViewFinderDisplayEventArgs : RoutedEventArgs
    {
        public ResolveViewFinderDisplayEventArgs()
        {
            RoutedEvent = Zoombox.ResolveViewFinderDisplayEvent;
        }

        public ZoomboxViewFinderDisplay ResolvedViewFinderDisplay { get; set; }
    }
}
