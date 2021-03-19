using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Logging
{
    [ProviderAlias("SszLogger")]
    public sealed class SszLoggerProvider : ILoggerProvider
    {
        private SszLoggerOptions _options;
        private readonly ConcurrentDictionary<string, SszLogger> _loggers =
            new ConcurrentDictionary<string, SszLogger>();

        public SszLoggerProvider(SszLoggerOptions options) =>
            _options = options;

        public SszLoggerProvider(IOptionsMonitor<SszLoggerOptions> options) :
            this(options.CurrentValue)
        {
            options.OnChange(options => {
                _options = options;
                foreach (var logger in _loggers.Values)
                {
                    logger.Options = options;
                }
            });
        }

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new SszLogger(name, _options));

        public void Dispose()
        {
            foreach (var logger in _loggers.Values)
            {
                logger.Dispose();
            }
            _loggers.Clear();
        }
    }
}
