using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Markup;

[assembly: XmlnsPrefix("urn:fluent-ribbon", "fluent")]
[assembly: XmlnsDefinition("urn:fluent-ribbon", "Fluent")]
[assembly: XmlnsDefinition("urn:fluent-ribbon", "Fluent.Converters")]
[assembly: XmlnsDefinition("urn:fluent-ribbon", "Fluent.TemplateSelectors")]
[assembly: XmlnsDefinition("urn:fluent-ribbon", "Fluent.Theming")]
[assembly: XmlnsDefinition("urn:fluent-ribbon", "Fluent.Metro.Behaviours")]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page,
                                              // app, or any theme specific resource dictionaries)
)]