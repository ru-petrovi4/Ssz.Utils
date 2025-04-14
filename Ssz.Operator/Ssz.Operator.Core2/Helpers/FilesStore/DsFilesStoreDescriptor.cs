using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    /// <summary>
    ///     Corresponds descriptor file 'Title=Экраны РСУ и Обходчика&amp;Path=SU1_Interface%5CSU1_Interface'
    /// </summary>
    public class DsFilesStoreDescriptor
    {
        #region construction and destruction

        public DsFilesStoreDescriptor(DsFilesStoreDirectory descriptorFileParentDsFilesStoreDirectory, DsFilesStoreFile descriptorDsFileInfo)
        {
            DescriptorFileParentDsFilesStoreDirectory = descriptorFileParentDsFilesStoreDirectory;
            DescriptorDsFileInfo = descriptorDsFileInfo;

            NameValuesCollection = NameValueCollectionHelper.Parse(Path.GetFileNameWithoutExtension(DescriptorDsFileInfo.Name));

            RelativeToDescriptorFileOrDirectoryPath = NameValuesCollection.TryGetValue(@"Path") ?? @"";
            Title = NameValuesCollection.TryGetValue(@"Title") ?? @"";
            CommandLine = NameValuesCollection.TryGetValue(@"CommandLine") ?? @"";
        }

        #endregion

        #region public functions

        /// <summary>
        ///     
        /// </summary>
        public DsFilesStoreDirectory DescriptorFileParentDsFilesStoreDirectory { get; }

        public DsFilesStoreFile DescriptorDsFileInfo { get; }

        public CaseInsensitiveDictionary<string?> NameValuesCollection { get; }

        /// <summary>
        ///     Relative to some directory path to file or directory
        ///     No '\' at the begin, no '\' at the end.
        /// </summary>
        public string RelativeToDescriptorFileOrDirectoryPath { get; }

        public string Title { get; }

        public string CommandLine { get; }

        #endregion
    }
}
