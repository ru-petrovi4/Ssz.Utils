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
        bool RefreshForPropertyGridIsDisabled { get; set; }

        void RefreshForPropertyGrid();

        void EndEditInPropertyGrid();
    }
}
