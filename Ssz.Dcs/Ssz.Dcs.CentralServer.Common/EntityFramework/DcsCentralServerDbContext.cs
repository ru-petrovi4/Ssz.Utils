using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ssz.Utils;
using System;
using System.IO;

namespace Ssz.Dcs.CentralServer.Common.EntityFramework
{
    public class SqliteDcsCentralServerDbContext : DcsCentralServerDbContext
    {
        #region construction and destruction

        /// <summary>
        ///     Nullable params for design-time tools.
        /// </summary>
        /// <param name="configuration"></param>
        public SqliteDcsCentralServerDbContext(IConfiguration? configuration = null)
        {
            _configuration = configuration;
        }

        #endregion

        #region protected functions

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string programDataDirectoryFullName = ConfigurationHelper.GetProgramDataDirectoryFullName(_configuration);

            optionsBuilder.UseSqlite("Data Source=" + Path.Combine(programDataDirectoryFullName, @"DcsCentralServer.db"),
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .EnableThreadSafetyChecks(false);
        }

        #endregion

        #region private fields

        private IConfiguration? _configuration;

        #endregion
    }

    public class NpgsqlDcsCentralServerDbContext : DcsCentralServerDbContext
    {
        #region construction and destruction

        /// <summary>
        ///     Nullable params for design-time tools.
        /// </summary>
        /// <param name="configuration"></param>
        public NpgsqlDcsCentralServerDbContext(IConfiguration? configuration = null)
        {
            _configuration = configuration;
        }

        #endregion

        #region protected functions

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(_configuration?.GetConnectionString("MainDbConnection"),
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                        .EnableThreadSafetyChecks(false);
        }

        #endregion

        #region private fields

        private IConfiguration? _configuration;

        #endregion
    }

    /// <summary>
    ///     To add migration: 
    ///     Startup project - Ssz.Dcs.CentralServer    
    ///     Default project of Package Management Console (PMC) - Ssz.Dcs.CentralServer
    ///     In PMC - 'add-migration name_of_migration'
    ///     To update the DB:
    ///     In PMC - 'update-database'
    /// </summary>
    public class DcsCentralServerDbContext : DbContext
    {
        #region construction and destruction

        /// <summary>
        ///     Nullable params for design-time tools.
        /// </summary>
        /// <param name="configuration"></param>
        public DcsCentralServerDbContext(IConfiguration? configuration = null)
        {
            _configuration = configuration;
        }

        #endregion

        #region public functions      
        
        public bool IsConfigured
        {
            get
            {
                if (_configuration is not null)
                {
                    string dbType = ConfigurationHelper.GetValue(_configuration, @"DbType", @"");
                    if (String.Equals(dbType, @"postgres", StringComparison.InvariantCultureIgnoreCase))
                        return true;
                    else if (String.Equals(dbType, @"sqlite", StringComparison.InvariantCultureIgnoreCase))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public DbSet<User> Users { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public DbSet<ProcessModelingSession> ProcessModelingSessions { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public DbSet<OperatorSession> OperatorSessions { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public DbSet<OpCompOperator> OpCompOperators { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public DbSet<ScenarioResult> ScenarioResults { get; set; } = null!;        

        /// <summary>
        /// 
        /// </summary>
        public DbSet<ProcessModel> ProcessModels { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public DbSet<Scenario> Scenarios { get; set; } = null!;

        #endregion

        #region protected functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (_configuration is not null)
            {
                string dbType = ConfigurationHelper.GetValue(_configuration, @"DbType", @"");
                if (String.Equals(dbType, @"postgres", StringComparison.InvariantCultureIgnoreCase))
                {
                    optionsBuilder.UseNpgsql(_configuration.GetConnectionString("MainDbConnection"),
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                        .EnableThreadSafetyChecks(false);
                }
                else if (String.Equals(dbType, @"sqlite", StringComparison.InvariantCultureIgnoreCase))
                {
                    string programDataDirectoryFullName = ConfigurationHelper.GetProgramDataDirectoryFullName(_configuration);

                    optionsBuilder.UseSqlite("Data Source=" + Path.Combine(programDataDirectoryFullName, @"DcsCentralServer.db"),
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                        .EnableThreadSafetyChecks(false);                    
                }
            }            
        }

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.UseIdentityColumns();
        //}

        #endregion

        #region private fields

        private IConfiguration? _configuration;

        #endregion
    }    
}