using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public static class DsFilesStoreHelper
    {
        #region public functions        

        public static string GetDirectoryPath_RelativeToProcessModelDirectory_OfSavesDirectory(string creatorUserName)
        {
            if (creatorUserName == @"")
                return DsFilesStoreConstants.SavesDirectoryNameUpper;
            else
                return Path.Combine(DsFilesStoreConstants.SavesDirectoryNameUpper, GetDsFilesStoreDirectoryName(creatorUserName));
        }

        public static string GetDsFilesStoreDirectoryName(string userName)
        {            
            return "'" + userName + "'";
        }

        public static string GetUserName(string dsFilesStoreDirectoryName)
        {
            return dsFilesStoreDirectoryName.Substring(1, dsFilesStoreDirectoryName.Length - 2);
        }


        public static int GetDsFilesStoreDirectoryFilesCount(DsFilesStoreDirectory? dsFilesStoreDirectory)
        {
            if (dsFilesStoreDirectory is null) return 0;

            int result = dsFilesStoreDirectory.DsFilesStoreFilesCollection.Count;
            foreach (var childDsFilesStoreDirectory in dsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection)
            {
                result += GetDsFilesStoreDirectoryFilesCount(childDsFilesStoreDirectory);
            }
            return result;
        }

        public static DsFilesStoreDirectoryType GetDsFilesStoreDirectoryType(DsFilesStoreDirectory dsFilesStoreDirectory)
        {
            switch (dsFilesStoreDirectory.Name.ToUpperInvariant())
            {
                case DsFilesStoreConstants.SavesDirectoryNameUpper:
                    return DsFilesStoreDirectoryType.Saves;                
                case DsFilesStoreConstants.InstructorBinDirectoryNameUpper:
                    return DsFilesStoreDirectoryType.InstructorBin;
                case DsFilesStoreConstants.InstructorDataDirectoryNameUpper:
                    return DsFilesStoreDirectoryType.InstructorData;
                case DsFilesStoreConstants.ControlEngineBinDirectoryNameUpper:
                    return DsFilesStoreDirectoryType.ControlEngineBin;
                case DsFilesStoreConstants.ControlEngineDataDirectoryNameUpper:
                    return DsFilesStoreDirectoryType.ControlEngineData;
                case DsFilesStoreConstants.OperatorBinDirectoryNameUpper:
                    return DsFilesStoreDirectoryType.OperatorBin;
                case DsFilesStoreConstants.OperatorDataDirectoryNameUpper:
                    return DsFilesStoreDirectoryType.OperatorData;
                case DsFilesStoreConstants.PlatInstructorBinDirectoryNameUpper:
                    return DsFilesStoreDirectoryType.PlatInstructorBin;
                case DsFilesStoreConstants.PlatInstructorDataDirectoryNameUpper:
                    return DsFilesStoreDirectoryType.PlatInstructorData;                
                default:
                    return DsFilesStoreDirectoryType.General;
            }           
        }

        public static string GetDsFilesStoreDirectoryName(DsFilesStoreDirectoryType dsFilesStoreDirectoryType)
        {
            switch (dsFilesStoreDirectoryType)
            {
                case DsFilesStoreDirectoryType.Saves:
                    return DsFilesStoreConstants.SavesDirectoryName;
                case DsFilesStoreDirectoryType.InstructorBin:
                    return DsFilesStoreConstants.InstructorBinDirectoryName;
                case DsFilesStoreDirectoryType.InstructorData:
                    return DsFilesStoreConstants.InstructorDataDirectoryName;
                case DsFilesStoreDirectoryType.ControlEngineBin:
                    return DsFilesStoreConstants.ControlEngineBinDirectoryName;
                case DsFilesStoreDirectoryType.ControlEngineData:
                    return DsFilesStoreConstants.ControlEngineDataDirectoryName;
                case DsFilesStoreDirectoryType.OperatorBin:
                    return DsFilesStoreConstants.OperatorBinDirectoryName;
                case DsFilesStoreDirectoryType.OperatorData:
                    return DsFilesStoreConstants.OperatorDataDirectoryName;
                case DsFilesStoreDirectoryType.PlatInstructorBin:
                    return DsFilesStoreConstants.PlatInstructorBinDirectoryName;
                case DsFilesStoreDirectoryType.PlatInstructorData:
                    return DsFilesStoreConstants.PlatInstructorDataDirectoryName;
                default:
                    throw new InvalidOperationException();
            }
        }

        /// <summary>
        ///     dsFilesStoreDirectory is directory with descriptor files.
        ///     Logs with Debug priority.
        /// </summary>
        /// <param name="dsFilesStoreDirectory"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static DsFilesStoreDescriptor[] GetDsFilesStoreDescriptors(DsFilesStoreDirectory dsFilesStoreDirectory, ILogger? logger = null)
        {
            var result = new List<DsFilesStoreDescriptor>();

            foreach (DsFilesStoreFile dsFileInfo in dsFilesStoreDirectory.DsFilesStoreFilesCollection)
            {
                if (!dsFileInfo.Name.EndsWith(@".csv", StringComparison.InvariantCultureIgnoreCase)) continue;

                result.Add(new DsFilesStoreDescriptor(dsFilesStoreDirectory, dsFileInfo));
            }

            return result.ToArray();
        }        

        /// <summary>
        ///     pathRelativeToRootDirectory:
        ///     Relative path to the root of the Files Store.
        ///     No '\' at the begin, no '\' at the end.
        ///     String.Empty for the Files Store root directory.
        /// </summary>
        /// <param name="rootDirectoryInfo"></param>
        /// <param name="pathRelativeToRootDirectory"></param>
        /// <param name="filesAndDirectoriesIncludeLevel"></param>
        /// <returns></returns>
        public static DsFilesStoreDirectory CreateDsFilesStoreDirectoryObject(DirectoryInfo rootDirectoryInfo, string pathRelativeToRootDirectory,
            int filesAndDirectoriesIncludeLevel = Int32.MaxValue)
        {
            var currentDirectoryInfo = Directory.CreateDirectory(Path.Combine(rootDirectoryInfo.FullName, pathRelativeToRootDirectory));
            return CreateDsFilesStoreDirectoryObjectInternal(currentDirectoryInfo,
                pathRelativeToRootDirectory, filesAndDirectoriesIncludeLevel);
        }

        /// <summary>
        ///     Finds directory of requested type in process model directory or in root directory.
        ///     Returns null if invalid argument or no directory. 
        /// </summary>
        /// <param name="rootDsFilesStoreDirectory"></param>
        /// <param name="pocessModelDsFilesStoreDirectory"></param>
        /// <param name="dsFilesStoreDirectoryType"></param>
        /// <returns></returns>
        public static DsFilesStoreDirectory? FindBinDsFilesStoreDirectory(DsFilesStoreDirectory rootDsFilesStoreDirectory, DsFilesStoreDirectory pocessModelDsFilesStoreDirectory, DsFilesStoreDirectoryType dsFilesStoreDirectoryType)
        {            
            DsFilesStoreDirectory? binDsFilesStoreDirectory = pocessModelDsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection.FirstOrDefault(d => GetDsFilesStoreDirectoryType(d) == dsFilesStoreDirectoryType);
            if (binDsFilesStoreDirectory is null)
                binDsFilesStoreDirectory = rootDsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection.FirstOrDefault(d => GetDsFilesStoreDirectoryType(d) == dsFilesStoreDirectoryType);
            return binDsFilesStoreDirectory;
        }

        /// <summary>
        ///     Finds directory of requested type in process model directory.
        ///     Returns null if invalid argument or no directory. 
        /// </summary>
        /// <param name="pocessModelDsFilesStoreDirectory"></param>
        /// <param name="dsFilesStoreDirectoryType"></param>
        /// <returns></returns>
        public static DsFilesStoreDirectory? FindDataDsFilesStoreDirectory(DsFilesStoreDirectory pocessModelDsFilesStoreDirectory, DsFilesStoreDirectoryType dsFilesStoreDirectoryType)
        {            
            return pocessModelDsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection.FirstOrDefault(d => GetDsFilesStoreDirectoryType(d) == dsFilesStoreDirectoryType);            
        }

        public static DsFilesStoreItem? FindDsFilesStoreItem(DsFilesStoreDirectory dsFilesStoreDirectory, string relativeToDirectoryPath)
        {
            if (relativeToDirectoryPath == @"") return new DsFilesStoreItem(dsFilesStoreDirectory, null);

            bool found = false;
            DsFilesStoreFile? dsFilesStoreFile = null;
            var parts = relativeToDirectoryPath.Split('\\', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (i < parts.Length - 1)
                {
                    var d = dsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection.FirstOrDefault(ddi => String.Equals(ddi.Name, p, StringComparison.InvariantCultureIgnoreCase));
                    if (d is null) break;
                    dsFilesStoreDirectory = d;
                }
                else
                {
                    var f = dsFilesStoreDirectory.DsFilesStoreFilesCollection.FirstOrDefault(dfi => String.Equals(dfi.Name, p, StringComparison.InvariantCultureIgnoreCase));
                    if (f is not null)
                    {
                        found = true;
                        dsFilesStoreFile = f;
                    }
                    else
                    {
                        var d = dsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection.FirstOrDefault(ddi => String.Equals(ddi.Name, p, StringComparison.InvariantCultureIgnoreCase));
                        if (d is not null)
                        {
                            found = true;
                            dsFilesStoreDirectory = d;
                        }
                    }
                }
            }
            if (!found)
                return null;
            else
                return new DsFilesStoreItem(dsFilesStoreDirectory, dsFilesStoreFile);
        }

        #endregion

        #region private functions

        public static DsFilesStoreDirectory CreateDsFilesStoreDirectoryObjectInternal(DirectoryInfo currentDirectoryInfo, string pathRelativeToRootDirectory,
            int filesAndDirectoriesIncludeLevel)
        {
            var dsFilesStoreDirectory = new DsFilesStoreDirectory();
            if (pathRelativeToRootDirectory != @"") // Not Root directory
            {                
                dsFilesStoreDirectory.PathRelativeToRootDirectory = pathRelativeToRootDirectory;
            }

            if (filesAndDirectoriesIncludeLevel > 0)
            {
                filesAndDirectoriesIncludeLevel -= 1;
                foreach (DirectoryInfo childDirectoryInfo in currentDirectoryInfo.EnumerateDirectories())
                {
                    string childPathRelativeToRootDirectory;
                    if (pathRelativeToRootDirectory == @"")
                        childPathRelativeToRootDirectory = childDirectoryInfo.Name;
                    else
                        childPathRelativeToRootDirectory = pathRelativeToRootDirectory + @"\" + childDirectoryInfo.Name;
                    dsFilesStoreDirectory.ChildDsFilesStoreDirectoriesCollection.Add(CreateDsFilesStoreDirectoryObjectInternal(childDirectoryInfo, childPathRelativeToRootDirectory,
                        filesAndDirectoriesIncludeLevel));
                }

                foreach (FileInfo fileInfo in currentDirectoryInfo.EnumerateFiles())
                {
                    var dsFileInfo = new DsFilesStoreFile();
                    dsFileInfo.Name = fileInfo.Name;
                    dsFileInfo.LastWriteTimeUtc = fileInfo.LastWriteTimeUtc;
                    dsFilesStoreDirectory.DsFilesStoreFilesCollection.Add(dsFileInfo);
                }
            }

            return dsFilesStoreDirectory;
        }

        #endregion
    }

    public enum DsFilesStoreDirectoryType
    {
        General,        
        Saves,        
        InstructorBin,
        InstructorData,
        ControlEngineBin,
        ControlEngineData,
        OperatorBin,
        OperatorData,
        PlatInstructorBin,
        PlatInstructorData,
    }
}


//if (dsFilesStoreDirectory.DsFilesStoreFilesCollection.Count == 0)
//    return DsFilesStoreDirectoryType.General;

//if (dsFilesStoreDirectory.DsFilesStoreFilesCollection.FirstOrDefault(f => String.Equals(f.Name, @"PlatInstructor.exe", StringComparison.InvariantCultureIgnoreCase)) is not null)
//    return DsFilesStoreDirectoryType.PlatInstructorEngineBin;

//if (dsFilesStoreDirectory.DsFilesStoreFilesCollection.FirstOrDefault(f => f.Name.EndsWith(@".mv_", StringComparison.InvariantCultureIgnoreCase)) is not null)
//    return DsFilesStoreDirectoryType.PlatInstructorEngineData;

//if (dsFilesStoreDirectory.DsFilesStoreFilesCollection.FirstOrDefault(f => String.Equals(f.Name, @"Ssz.Dcs.Operator.Play.exe", StringComparison.InvariantCultureIgnoreCase)) is not null)
//    return DsFilesStoreDirectoryType.OperatorBin;

//if (dsFilesStoreDirectory.DsFilesStoreFilesCollection.FirstOrDefault(f => f.Name.Contains(@".dssolution.NameToDisplay=", StringComparison.InvariantCultureIgnoreCase)) is not null)
//    return DsFilesStoreDirectoryType.OperatorData;


//var index = dsFileInfo.Name.IndexOf('~');
//if (index == -1) continue;
//string nameToDisplay = dsFileInfo.Name.Substring(@"NameToDisplay=".Length, index - @"NameToDisplay=".Length);
//string relativePath = dsFilesStoreDirectory.RelativePath + dsFileInfo.Name.Substring(index + 1).Replace('~', '\\');

//var d = FindDirecto

