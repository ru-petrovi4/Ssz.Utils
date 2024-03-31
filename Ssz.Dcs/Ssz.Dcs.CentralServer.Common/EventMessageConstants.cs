using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Ssz.Dcs.CentralServer.Common
{
    public static class EventMessageConstants
    {
        public static readonly TypeId PrepareAndRunInstructorExe_TypeId = new("", "Ssz.Dcs", "PrepareAndRunInstructorExe");

        public static readonly TypeId RunInstructorExe_TypeId = new("", "Ssz.Dcs", "RunInstructorExe");

        public static readonly TypeId PrepareAndRunOperatorExe_TypeId = new("", "Ssz.Dcs", "LaunchOperator");

        public static readonly TypeId RunOperatorExe_TypeId = new("", "Ssz.Dcs", "RunOperatorExe");        

        public static readonly TypeId PrepareAndRunEngineExe_TypeId = new("", "Ssz.Dcs", "LaunchEngine");

        public static readonly TypeId DownloadChangedFiles_TypeId = new("", "Ssz.Dcs", "DownloadChangedFiles");

        /// <summary>
        ///     !!!Warning!!! Does not upload child directories!!!
        /// </summary>
        public static readonly TypeId UploadChangedFiles_TypeId = new("", "Ssz.Dcs", "UploadChangedFiles");

        public static readonly TypeId ExternalEvent_TypeId = new("", "Ssz.Dcs", "ExternalEvent");
    }
}
