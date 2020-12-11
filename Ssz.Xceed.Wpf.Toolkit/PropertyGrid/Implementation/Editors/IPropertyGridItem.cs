using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors
{
    public interface IPropertyGridItem
    {
        bool PropertyGridRefreshDisabled { get; set; }

        void OnPropertyGridRefresh();

        void OnEndEditing();
    }
}
