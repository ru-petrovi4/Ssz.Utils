using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Ssz.Utils.Yaml
{
    internal class StaticConfigurationSource: IConfigurationSource
    {
        public IDictionary<string, string?> Data { get; set; } = null!;

        public IConfigurationProvider Build(IConfigurationBuilder builder)
            => new StaticConfigurationProvider(Data);
    }
}