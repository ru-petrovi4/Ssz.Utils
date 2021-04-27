using System.Windows;
using System.Windows.Markup;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.SourceAssembly, //where theme specific resource dictionaries are located
    //(used if a resource is not found in the page, 
    // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
    //(used if a resource is not found in the page, 
    // app, or any theme specific resource dictionaries)
)]

[assembly: XmlnsPrefix("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "xctk")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.Core.Converters")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.Core.Input")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.Core.Media")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.Core.Utilities")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.Chromes")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.Primitives")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.PropertyGrid")]
[assembly:
    XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes")]
[assembly:
    XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Commands")]
[assembly:
    XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Converters")]
[assembly:
    XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.Zoombox")]
[assembly: XmlnsDefinition("http://schemas.xceed.com/wpf/xaml/toolkit/Ssz", "Ssz.Xceed.Wpf.Toolkit.Panels")]