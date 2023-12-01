using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Ssz.Utils.Properties;
using YamlDotNet.Core;

namespace Ssz.Utils.Yaml
{
    /// <summary>
    /// A YAML file based <see cref="FileConfigurationProvider"/>.
    /// </summary>
    public class YamlConfigurationProvider : FileConfigurationProvider
    {
        public YamlConfigurationProvider(YamlConfigurationSource source) : base(source) { }

        public override void Load(Stream stream)
        {
            var parser = new YamlConfigurationStreamParser();
            try
            {
                Data = parser.Parse(stream);
            }
            catch (YamlException e)
            {
                throw new FormatException(String.Format(Resources.Error_YamlParseError, e.Start.Line, e.Start.Column) + " " + e.Message);
            }
        }
    }
}
