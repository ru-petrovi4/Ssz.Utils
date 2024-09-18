using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public static class ProcessModelingSessionConstants
    {
        public const int Initiated = 0;

        public const int InstructorConnected = 1;

        public const int InstructorDisconnected = 2;

        public const string Exam_Mode = @"Exam";

        public const string Training_Mode = @"Training";

        public const string LocalTraining_Mode = @"LocalTraining";

        public const string ProcessModelConfig_Mode = @"ProcessModelConfig";

        public const string Instructor_RoleId = @"-1";

        public const string DefaultTrainee_RoleId = @"0";        

        public const int EventSubType_ChangeElementValue = 1;

        public const int EventSubType_LoadStateFile = 2;
    }
}
