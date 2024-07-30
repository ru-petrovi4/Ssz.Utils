using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Ssz.Utils;
using System;
using System.IO;

namespace Ssz.Dcs.CentralServer.Common.EntityFramework
{
    /// <summary>
    /// 
    /// </summary>
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
            optionsBuilder.UseSqlite(@"Data Source=DcsCentralServer.db",
                    o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                .EnableThreadSafetyChecks(false);
        }

        #endregion

        #region private fields

        private IConfiguration? _configuration;

        #endregion
    }

    /// <summary>
    /// 
    /// </summary>
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
    /// 
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
                    string dbType = ConfigurationHelper.GetValue(_configuration, DbConstants.ConfigurationKey_DbType, @"");
                    if (String.Equals(dbType, DbConstants.ConfigurationValue_DbType_Postgres, StringComparison.InvariantCultureIgnoreCase))
                        return true;
                    else if (String.Equals(dbType, DbConstants.ConfigurationValue_DbType_Sqlite, StringComparison.InvariantCultureIgnoreCase))
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
                string dbType = ConfigurationHelper.GetValue(_configuration, DbConstants.ConfigurationKey_DbType, @"");
                if (String.Equals(dbType, DbConstants.ConfigurationValue_DbType_Postgres, StringComparison.InvariantCultureIgnoreCase))
                {
                    optionsBuilder.UseNpgsql(_configuration.GetConnectionString("MainDbConnection"),
                            o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
                        .EnableThreadSafetyChecks(false);
                }
                else if (String.Equals(dbType, DbConstants.ConfigurationValue_DbType_Sqlite, StringComparison.InvariantCultureIgnoreCase))
                {
                    optionsBuilder.UseSqlite(@"Data Source=DcsCentralServer.db",
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