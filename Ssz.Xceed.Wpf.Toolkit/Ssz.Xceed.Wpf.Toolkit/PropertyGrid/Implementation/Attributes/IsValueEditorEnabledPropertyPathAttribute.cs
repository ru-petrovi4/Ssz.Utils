using System;

namespace Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes
{
    public class IsValueEditorEnabledPropertyPathAttribute : Attribute
    {
        public IsValueEditorEnabledPropertyPathAttribute(string path)
        {
            Path = path;
        }

        public string Path { get; set; }
    }
}