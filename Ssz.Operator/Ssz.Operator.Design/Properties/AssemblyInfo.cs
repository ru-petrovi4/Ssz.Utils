using System.Runtime.Versioning;
using System.Windows;

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the dsPage,
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the dsPage,
                                              // app, or any theme specific resource dictionaries)
)]

#if NET5_0_OR_GREATER
[assembly: SupportedOSPlatform("windows7.0")]
#endif