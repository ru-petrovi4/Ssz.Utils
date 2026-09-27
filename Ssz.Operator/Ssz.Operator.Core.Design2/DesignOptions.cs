using Microsoft.Extensions.Configuration;
using Ssz.Utils;

namespace Ssz.Operator.Core.Design;

/// <summary>
///     What the editor was started with.
///     <para>
///         Ported from the Options class of the WPF editor's MainWindow, with the same names, so a
///         command line written for that editor works here.
///     </para>
/// </summary>
public sealed class DesignOptions
{
    #region construction and destruction

    public DesignOptions(IConfiguration? configuration)
    {
        ProjectFile = ConfigurationHelper.GetValue<string>(configuration, @"ProjectFile", @"");
        AutoConvert = ConfigurationHelper.GetValue<bool>(configuration, @"AutoConvert", false);
        ToolkitOperation = ConfigurationHelper.GetValue<string>(configuration, @"ToolkitOperation", @"");
        ToolkitOperationsSilent =
            ConfigurationHelper.GetValue<bool>(configuration, @"ToolkitOperationsSilent", false);
        Options_ = ConfigurationHelper.GetValue<string>(configuration, @"Options", @"");
    }

    #endregion

    #region public functions

    public string ProjectFile { get; set; }

    public bool AutoConvert { get; set; }

    public string ToolkitOperation { get; set; }

    public bool ToolkitOperationsSilent { get; set; }

    public string Options_ { get; set; }

    #endregion
}
