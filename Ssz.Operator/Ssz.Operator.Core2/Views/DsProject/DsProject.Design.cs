using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     The directories the project keeps its drawings in, which only the editor needs.
    ///     <para>
    ///         Ported from the WPF project file. These work on a drive, so in the browser - where the
    ///         project comes from a file provider - they answer with nothing rather than failing.
    ///     </para>
    /// </summary>
    public partial class DsProject
    {
        #region public functions

        [Browsable(false)]
        public DirectoryInfo? DsPagesDirectoryInfo => GetOrCreateSubdirectory(@"Pages");

        [Browsable(false)]
        public DirectoryInfo? DsShapesDirectoryInfo => GetOrCreateSubdirectory(@"Shapes");

        #endregion

        #region private functions

        private DirectoryInfo? GetOrCreateSubdirectory(string name)
        {
            if (!IsInitialized || IsBrowserMode(Mode)) return null;

            DirectoryInfo? directoryInfo = null;
            try
            {
                directoryInfo = new DirectoryInfo(DsProjectPath)
                    .EnumerateDirectories(name, SearchOption.TopDirectoryOnly)
                    .FirstOrDefault();
            }
            catch (Exception)
            {
            }

            if (directoryInfo is not null) return directoryInfo;

            if (IsReadOnly) return null;
            try
            {
                return new DirectoryInfo(DsProjectPath).CreateSubdirectory(name);
            }
            catch (Exception ex)
            {
                LoggersSet.Logger.LogCritical(ex, @"Create Subdirectory " + name + @" failed.");
                return null;
            }
        }

        #endregion
    }
}
