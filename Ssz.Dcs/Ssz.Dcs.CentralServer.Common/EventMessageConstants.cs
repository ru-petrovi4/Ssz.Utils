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
        public static readonly TypeId LaunchInstructor_TypeId = new("", "Ssz.Dcs", "LaunchInstructor");

        public static readonly TypeId LaunchOperator_TypeId = new("", "Ssz.Dcs", "LaunchOperator");

        public static readonly TypeId RunInstructorExe_TypeId = new("", "Ssz.Dcs", "RunInstructorExe");

        public static readonly TypeId RunOperatorExe_TypeId = new("", "Ssz.Dcs", "RunOperatorExe");        

        public static readonly TypeId LaunchEngine_TypeId = new("", "Ssz.Dcs", "LaunchEngine");

        public static readonly TypeId DownloadChangedFiles_TypeId = new("", "Ssz.Dcs", "DownloadChangedFiles");

        /// <summary>
        ///     !!!Warning!!! Not Uploads child directories!!!
        /// </summary>
        public static readonly TypeId UploadChangedFiles_TypeId = new("", "Ssz.Dcs", "UploadChangedFiles");

        public static readonly TypeId ExternalEvent_TypeId = new("", "Ssz.Dcs", "ExternalEvent");
    }
}
