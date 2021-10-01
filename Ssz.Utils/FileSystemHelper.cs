using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Ssz.Utils
{
    public static class FileSystemHelper
    {
        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="targetPath"></param>
        public static void MoveDirectory(string sourcePath, string targetPath)
        {
            sourcePath = sourcePath.TrimEnd('\\');
            targetPath = targetPath.TrimEnd('\\');
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s) ?? @"");
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath, StringComparison.InvariantCultureIgnoreCase);
                //     Creates all directories and subdirectories in the specified path unless they
                //     already exist.
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));                   
                    File.Move(file, targetFile, true);
                }
            }
            Directory.Delete(sourcePath, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileOrDirectoryName"></param>
        /// <returns></returns>
        public static string ReplaceInvalidChars(string fileOrDirectoryName)
        {
            return string.Join("_", fileOrDirectoryName.Split(Path.GetInvalidFileNameChars()));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileOrDirectoryName"></param>
        /// <param name="throwIfFails"></param>
        /// <returns></returns>
        public static bool IsFileNameIsValid(string fileOrDirectoryName, bool throwIfFails = false)
        {
            try
            {
                //     Creates or overwrites a file in the specified path, specifying a buffer size
                //     and options that describe how to create or overwrite the file.
                using (FileStream fs = File.Create(
                    Path.Combine(Path.GetTempPath(), fileOrDirectoryName),
                    1,
                    FileOptions.DeleteOnClose)
                    )
                {
                }
                return true;
            }
            catch
            {
                if (throwIfFails)
                    throw;
                else
                    return false;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="path"/> starts with the path <paramref name="baseDirPath"/>.
        /// The comparison is case-insensitive, handles / and \ slashes as folder separators and
        /// only matches if the base dir folder name is matched exactly ("c:\foobar\file.txt" is not a sub path of "c:\foo").
        /// </summary>
        public static bool IsSubPathOf(string path, string baseDirPath)
        {
            string normalizedPath = Path.GetFullPath(path.Replace('/', '\\')
                .WithEnding("\\"));

            string normalizedBaseDirPath = Path.GetFullPath(baseDirPath.Replace('/', '\\')
                .WithEnding("\\"));

            return normalizedPath.StartsWith(normalizedBaseDirPath, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pathLeft"></param>
        /// <param name="pathRight"></param>
        /// <returns></returns>
        public static bool Compare(string pathLeft, string pathRight)
        {
            string normalizedPathLeft = Path.GetFullPath(pathLeft.Replace('/', '\\')
                .WithEnding("\\"));

            string normalizedPathRight = Path.GetFullPath(pathRight.Replace('/', '\\')
                .WithEnding("\\"));

            return String.Equals(normalizedPathLeft, normalizedPathRight, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="searchOption"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public static IEnumerable<FileInfo> GetFilesByExtensions(this DirectoryInfo dir, SearchOption searchOption,
            params string[] extensions)
        {            
            IEnumerable<FileInfo> files = dir.EnumerateFiles("*", searchOption);
            return files.Where(f => extensions.Contains(f.Extension, StringComparer.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directoryPath"></param>
        /// <param name="throwIfFails"></param>
        /// <returns></returns>
        public static bool IsDirectoryWritable(string directoryPath, bool throwIfFails = false)
        {
            try
            {
                using (FileStream fs = File.Create(
                    Path.Combine(
                        directoryPath,
                        Path.GetRandomFileName()
                        ),
                    1,
                    FileOptions.DeleteOnClose)
                    )
                {
                }
                return true;
            }
            catch
            {
                if (throwIfFails)
                    throw;
                else
                    return false;
            }
        }

        #endregion
    }
}