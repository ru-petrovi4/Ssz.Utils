using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Logging
{
    public static class LoggingBuilderExtensions
    {
        static public ILoggingBuilder AddSszLogger(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider,
                                              SszLoggerProvider>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton
               <IConfigureOptions<SszLoggerOptions>, SszLoggerOptionsSetup>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton
               <IOptionsChangeTokenSource<SszLoggerOptions>,
               LoggerProviderOptionsChangeTokenSource<SszLoggerOptions, SszLoggerProvider>>());

            return builder;
        }

        static public ILoggingBuilder AddSszLogger
               (this ILoggingBuilder builder, Action<SszLoggerOptions> configure)
        {
            builder.AddSszLogger();
            builder.Services.Configure(configure);
            return builder;
        }

        internal class SszLoggerOptionsSetup : ConfigureFromConfigurationOptions<SszLoggerOptions>
        {
            public SszLoggerOptionsSetup(ILoggerProviderConfiguration<SszLoggerProvider>
                                          providerConfiguration)
                : base(providerConfiguration.Configuration)
            {
            }
        }
    }
}
