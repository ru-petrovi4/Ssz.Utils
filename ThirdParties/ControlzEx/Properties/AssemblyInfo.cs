using System;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Markup;

[assembly: XmlnsPrefix("urn:controlzex", "controlzex")]
[assembly: XmlnsDefinition("urn:controlzex", "ControlzEx")]
[assembly: XmlnsDefinition("urn:controlzex", "ControlzEx.Controls")]
[assembly: XmlnsDefinition("urn:controlzex", "ControlzEx.Behaviors")]
[assembly: XmlnsDefinition("urn:controlzex", "ControlzEx.Theming")]
[assembly: XmlnsDefinition("urn:controlzex", "ControlzEx.Windows.Shell")]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]