using Microsoft.Extensions.Configuration;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common.Helpers
{
    public static class ProgramDataDirectoryHelper
    {
        #region public functions        

        /// <summary>
        ///     If ProgramDataDirectory setting is not exist throws.
        ///     Otherwise, returns existing or creates new FilesStore subdirectory.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static DirectoryInfo GetFilesStoreDirectoryInfo(IConfiguration configuration)
        {
            string programDataDirectoryFullName = ConfigurationHelper.GetProgramDataDirectoryFullName(configuration);

            var filesStoreDirectoryFullName = Path.Combine(programDataDirectoryFullName, @"FilesStore");

            // Creates all directories and subdirectories in the specified path unless they already exist.
            Directory.CreateDirectory(filesStoreDirectoryFullName);

            return new DirectoryInfo(filesStoreDirectoryFullName);
        }

        /// <summary>
        ///     If DataDirectory is not exist throws.
        ///     Otherwise, returns existing or creates CsvDb subdirectory.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static DirectoryInfo GetCsvDbDirectoryInfo(IConfiguration configuration)
        {
            string programDataDirectoryFullName = ConfigurationHelper.GetProgramDataDirectoryFullName(configuration);

            var csvDbDirectoryFullName = Path.Combine(programDataDirectoryFullName, @"CsvDb");

            // Creates all directories and subdirectories in the specified path unless they already exist.
            Directory.CreateDirectory(csvDbDirectoryFullName);

            return new DirectoryInfo(csvDbDirectoryFullName);
        }

        #endregion
    }
}
