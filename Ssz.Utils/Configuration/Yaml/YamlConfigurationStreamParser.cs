using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Ssz.Utils.Properties;
using YamlDotNet.RepresentationModel;

namespace Ssz.Utils.Yaml
{
    public class YamlConfigurationStreamParser
    {
        #region public functions

        public IDictionary<string, string?> Parse(Stream input)
        {
            _data.Clear();
            _context.Clear();

            // https://dotnetfiddle.net/rrR2Bb
            var yaml = new YamlStream();
            using (var streamReader = new StreamReader(input, detectEncodingFromByteOrderMarks: true))
            {
                yaml.Load(streamReader);
            }

            if (yaml.Documents.Any())
            {
                var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

                // The document node is a mapping node
                VisitYamlMappingNode(mapping);
            }

            return _data;
        }

        public IDictionary<string, string?> Parse(YamlDocument yamlDocument)
        {
            _data.Clear();
            _context.Clear();            

            var mapping = (YamlMappingNode)yamlDocument.RootNode;

            // The document node is a mapping node
            VisitYamlMappingNode(mapping);

            return _data;
        }

        #endregion        

        #region private functions        

        private void VisitYamlNode(YamlNode yamlNode)
        {
            if (yamlNode is YamlScalarNode scalarNode)
            {                
                VisitYamlScalarNode(scalarNode);
            }
            if (yamlNode is YamlMappingNode mappingNode)
            {
                VisitYamlMappingNode(mappingNode);
            }
            if (yamlNode is YamlSequenceNode sequenceNode)
            {
                VisitYamlSequenceNode(sequenceNode);
            }
        }

        private void VisitYamlScalarNode(YamlScalarNode yamlScalarNode)
        {
            //a node with a single 1-1 mapping 

            var currentKey = _currentPath;

            if (_data.ContainsKey(currentKey))
            {
                throw new FormatException(String.Format(Resources.Error_YamlParseError_DuplicateKey, currentKey));
            }

            _data[currentKey] = IsNullValue(yamlScalarNode) ? null : yamlScalarNode.Value;            
        }

        private void VisitYamlMappingNode(YamlMappingNode yamlMappingNode)
        { 
            foreach (var yamlNodePair in yamlMappingNode.Children)
            {
                if (yamlNodePair.Key is YamlScalarNode yamlScalarNode)
                {
                    var context = yamlScalarNode.Value ?? @"";

                    EnterContext(context);

                    VisitYamlNode(yamlNodePair.Value);

                    ExitContext();
                }
            }
        }

        private void VisitYamlSequenceNode(YamlSequenceNode yamlSequenceNode)
        {
            //a node with an associated list            

            for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
            {
                EnterContext(i.ToString());

                VisitYamlNode(yamlSequenceNode.Children[i]);

                ExitContext();
            }            
        }        

        private void EnterContext(string context)
        {
            _context.Push(context);
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }

        private void ExitContext()
        {
            _context.Pop();
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }

        private bool IsNullValue(YamlScalarNode yamlScalarNode)
        {
            return yamlScalarNode.Style == YamlDotNet.Core.ScalarStyle.Plain
                && (
                    yamlScalarNode.Value == "~"
                    || yamlScalarNode.Value == "null"
                    || yamlScalarNode.Value == "Null"
                    || yamlScalarNode.Value == "NULL"
                );
        }

        #endregion

        #region private fields

        private readonly IDictionary<string, string?> _data = new SortedDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _context = new();
        private string _currentPath = @"";

        #endregion
    }
}
