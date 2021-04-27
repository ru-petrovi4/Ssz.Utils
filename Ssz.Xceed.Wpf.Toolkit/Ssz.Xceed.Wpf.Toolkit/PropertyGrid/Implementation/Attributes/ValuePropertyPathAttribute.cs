using System;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes
{
    public class ValuePropertyPathAttribute : Attribute
    {
        public ValuePropertyPathAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; set; }
    }
}