using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes
{
    public class ValuePropertyPathAttribute : Attribute
    {
        public ValuePropertyPathAttribute(string path)
        {
            Path = path;
        }

        public string Path
        {
            get;
            set;
        }
    }
}
