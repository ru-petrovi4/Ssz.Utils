using Ssz.Dcs.CentralServer.Common.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common.Helpers
{
    public static partial class DcsCentralServerDbHelper
    {
        #region public functions        

        public static void InitializeOrUpdateDb(IServiceProvider serviceProvider, IConfiguration configuration, ILoggersSet loggersSet)
        {
            string dbType = ConfigurationHelper.GetValue(configuration, @"DbType", @"");
            if (String.Equals(dbType, @"postgres", StringComparison.InvariantCultureIgnoreCase))
            {   
                using var dbContext = serviceProvider.GetRequiredService<IDbContextFactory<NpgsqlDcsCentralServerDbContext>>()
                    .CreateDbContext();

                // Applies any pending migrations for the context to the database. Will create the database
                // if it does not already exist.
                try
                {
                    dbContext.Database.Migrate();
                }
                catch
                {
                }

                InitializePostgresCrypto(dbContext);
            }
            else if (String.Equals(dbType, @"sqlite", StringComparison.InvariantCultureIgnoreCase))
            {
                using var dbContext = serviceProvider.GetRequiredService<IDbContextFactory<SqliteDcsCentralServerDbContext>>()
                    .CreateDbContext();

                // Applies any pending migrations for the context to the database. Will create the database
                // if it does not already exist.
                try
                {
                    dbContext.Database.Migrate();
                }
                catch
                {
                }
            }
            else
            {
                Console.WriteLine(Properties.Resources.DbTypeIsNotConfigured);
            }

            //var licenseFileInfo = new LicenseFileInfo
            //{
            //    LicenseOwner = @"TestUser",
            //    ModuleOrAddonLicenses = new[] { new ModuleOrAddonLicense
            //        {
            //            ModuleOrAddonGuid = new Guid(""),
            //            ModuleOrAddonIdentifier = @"Core",
            //            ModuleOrAddonDesc = @"Основной модуль",
            //            StartTimeUtc = DateTime.UtcNow,
            //            EndTimeUtc = DateTime.MaxValue,
            //            MaxUsers = 25,
            //        }
            //    }
            //};

            using var dbContext2 = serviceProvider.GetRequiredService<IDbContextFactory<DcsCentralServerDbContext>>()
                .CreateDbContext();

            if (dbContext2.IsConfigured) 
            {
                if (!dbContext2.Users.Any())
                {
                    var user = new User
                    {
                        UserName = DbConstants.DefaultInstructorUserName,
                    };
                    dbContext2.Users.Add(user);

                    user = new User
                    {
                        UserName = DbConstants.DefaultTraineeUserName,
                    };
                    dbContext2.Users.Add(user);
                }

                dbContext2.SaveChanges();
            }                      

            Console.WriteLine(Properties.Resources.DbInitializationSuccess);            
        }        

        #endregion

        #region private fields

        private static void InitializePostgresCrypto(DcsCentralServerDbContext dbContext)
        {
            try
            {
                // SQL injection safe
                //dbContext.Database.ExecuteSql($"CREATE EXTENSION pgcrypto SCHEMA public");
            }
            catch
            {
            }            
        }       

        #endregion
    }
}
