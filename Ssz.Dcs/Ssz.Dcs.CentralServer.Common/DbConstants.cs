using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public static class DbConstants
    {
        //public const string DefaultWindowsUserName = @"localhost\User";

        public const string UserName_DefaultInstructor = @"Тестовый Инструктор";

        public const string UserName_DefaultTrainee = @"Тестовый Оператор";

        public const string ConfigurationKey_DbType = @"DbType";

        public const string ConfigurationValue_DbType_Postgres = @"postgres";

        public const string ConfigurationValue_DbType_Sqlite = @"sqlite";
    }
}
