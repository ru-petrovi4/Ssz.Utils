using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Ssz.Utils.Yaml
{
    internal class StaticConfigurationProvider : ConfigurationProvider
    {
        public StaticConfigurationProvider(IDictionary<string, string?> data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
        }
    }
}