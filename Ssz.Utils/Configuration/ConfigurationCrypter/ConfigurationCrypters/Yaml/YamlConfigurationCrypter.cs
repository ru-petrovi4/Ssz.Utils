using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Ssz.Utils.ConfigurationCrypter.Crypters;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ssz.Utils.ConfigurationCrypter.ConfigurationCrypters.Yaml
{
    /// <summary>
    /// Config crypter that encrypts and decrypts keys in Yaml config files.
    /// </summary>
    public class YamlConfigurationCrypter : IConfigurationCrypter
    {
        #region construction and destruction

        /// <summary>
        /// Creates an instance of the YamlConfigCrypter.
        /// </summary>
        /// <param name="crypter">An ICrypter instance.</param>
        public YamlConfigurationCrypter(ICrypter crypter)
        {
            _crypter = crypter;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _crypter?.Dispose();
            }
        }

        #endregion

        #region public functions

        /// <summary>
        /// Encrypts the key in the given content of a config file.
        /// </summary>
        /// <param name="fileFullName">Сonfig file full name.</param>
        /// <param name="configKeys">Keys of the config entry. The key has to be in YamlPath format.</param>
        /// <returns>The content of the config file where the key has been encrypted.</returns>
        public void EncryptKeys(string fileFullName, HashSet<string> configKeys)
        {
            YamlStream yaml = new();
            using (var reader = new StreamReader(fileFullName, detectEncodingFromByteOrderMarks: true))
            {
                yaml.Load(reader);
            }

            if (yaml.Documents.Any())
            {
                var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

                // The document node is a mapping node
                VisitYamlMappingNode(mapping, configKeys, _crypter.EncryptString);
            }

            using (var writer = new StreamWriter(File.Create(fileFullName), new UTF8Encoding(true)))
            {
                yaml.Save(new Emitter(writer, 
                    new EmitterSettings(bestIndent: 2, bestWidth: int.MaxValue, isCanonical: false, maxSimpleKeyLength: 1024, skipAnchorName: true, indentSequences: false)), 
                    assignAnchors: false);
            }
        }

        /// <summary>
        /// Decrypts the key in the given content of a config file.
        /// </summary>
        /// <param name="fileFullName">Сonfig file full name.</param>
        /// <param name="configKeys">Keys of the config entry. The key has to be in YamlPath format.</param>
        /// <returns>The content of the config file where the key has been decrypted.</returns>
        public void DecryptKeys(string fileFullName, HashSet<string> configKeys)
        {
            YamlStream yaml = new();
            using (var reader = new StreamReader(fileFullName, detectEncodingFromByteOrderMarks: true))
            {
                yaml.Load(reader);
            }

            if (yaml.Documents.Any())
            {
                var mapping = (YamlMappingNode)yaml.Documents[0].RootNode;

                // The document node is a mapping node
                VisitYamlMappingNode(mapping, configKeys, _crypter.DecryptString);
            }

            using (var writer = new StreamWriter(File.Create(fileFullName), new UTF8Encoding(true)))
            {
                yaml.Save(new Emitter(writer,
                    new EmitterSettings(bestIndent: 2, bestWidth: int.MaxValue, isCanonical: false, maxSimpleKeyLength: 1024, skipAnchorName: true, indentSequences: false)),
                    assignAnchors: false);
            }
        }        

        #endregion

        #region private functions

        private void VisitYamlNode(YamlNode yamlNode, HashSet<string>? configKeys, Func<string?, string?> func)
        {
            if (yamlNode is YamlScalarNode scalarNode)
            {
                VisitYamlScalarNode(scalarNode, configKeys, func);
            }
            if (yamlNode is YamlMappingNode mappingNode)
            {
                VisitYamlMappingNode(mappingNode, configKeys, func);
            }
            if (yamlNode is YamlSequenceNode sequenceNode)
            {
                VisitYamlSequenceNode(sequenceNode, configKeys, func);
            }
        }

        private void VisitYamlScalarNode(YamlScalarNode yamlScalarNode, HashSet<string>? configKeys, Func<string?, string?> func)
        {
            //a node with a single 1-1 mapping 
            
            if (configKeys is null || configKeys.Contains(_currentPath))
            {                
                yamlScalarNode.Value = func(yamlScalarNode.Value);
            }

            yamlScalarNode.Style = ScalarStyle.DoubleQuoted;
        }

        private void VisitYamlMappingNode(YamlMappingNode yamlMappingNode, HashSet<string>? configKeys, Func<string?, string?> func)
        {
            foreach (var yamlNodePair in yamlMappingNode.Children)
            {
                var context = ((YamlScalarNode)yamlNodePair.Key).Value ?? @"";

                EnterContext(context);

                if (configKeys is not null && configKeys.Contains(_currentPath))
                    VisitYamlNode(yamlNodePair.Value, null, func);
                else
                    VisitYamlNode(yamlNodePair.Value, configKeys, func);

                ExitContext();
            }
        }

        private void VisitYamlSequenceNode(YamlSequenceNode yamlSequenceNode, HashSet<string>? configKeys, Func<string?, string?> func)
        {
            //a node with an associated list            

            for (int i = 0; i < yamlSequenceNode.Children.Count; i++)
            {
                EnterContext(i.ToString());

                if (configKeys is not null && configKeys.Contains(_currentPath))
                    VisitYamlNode(yamlSequenceNode.Children[i], null, func);
                else
                    VisitYamlNode(yamlSequenceNode.Children[i], configKeys, func);

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

        #endregion

        #region private fields

        private readonly ICrypter _crypter;
        private readonly Stack<string> _context = new();
        private string _currentPath = @"";

        #endregion
    }
}