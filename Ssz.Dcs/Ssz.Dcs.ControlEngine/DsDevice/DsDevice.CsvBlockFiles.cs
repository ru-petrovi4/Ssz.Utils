using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Ssz.Dcs.ControlEngine
{
    public partial class DsDevice : IDisposable
    {
        #region private functions

        private List<DsModule> LoadModulesFromCsv(ILogger? userFriendlyLogger, string csvDsBlockFileFullName)
        {
            var result = new List<DsModule>();
            string templateDsBlockTag = Path.GetFileName(csvDsBlockFileFullName);
            if (!templateDsBlockTag.EndsWith(".block.csv", StringComparison.InvariantCultureIgnoreCase)) return result;
            templateDsBlockTag = templateDsBlockTag.Substring(0, templateDsBlockTag.Length - ".block.csv".Length);

            var module = new DsModule(templateDsBlockTag, this);

            var templateComponentDsBlock = _modulesTempRuntimeData.ChildDsBlocksDictionary.TryGetValue(templateDsBlockTag) as ComponentDsBlock;
            if (templateComponentDsBlock is null || !templateComponentDsBlock.IsTemplate)
            {
                templateComponentDsBlock = DsBlocksFactory.CreateStandardTemplateComponentDsBlock(templateDsBlockTag, module, null, templateDsBlockTag);
            }
            if (templateComponentDsBlock is null || !templateComponentDsBlock.IsTemplate)
            {
                _logger.LogError("Cannot find template block: " + csvDsBlockFileFullName);
                return result;
            }

            var blocks = new List<DsBlockBase>(1024);

            var fileData = CsvDb.GetData(Path.GetFileName(csvDsBlockFileFullName));
            if (fileData == null)
                return result;
            var headerValues = fileData.TryGetValue(@"");
            if (headerValues == null)
                return result;
            foreach (var values in fileData.Values)
            {                
                var tag = values[0];
                if (String.IsNullOrEmpty(tag)) continue;
                string desc = @"";
                if (values.Count >= 2) desc = values[1] ?? @"";

                var componentDsBlock = SerializationHelper.CloneUsingSerialization(templateComponentDsBlock,
                    () => DsBlocksFactory.CreateComponentDsBlock(tag, module, null));
                componentDsBlock.IsTemplate = false;
                componentDsBlock.CreatedFromTemplate = templateDsBlockTag;
                componentDsBlock.DESC.Value.Set(desc);
                for (int index = 2; index < headerValues.Count + 2; index++)
                {
                    if (index >= values.Count) break;
                    string? paramAliasString = headerValues[index];
                    if (String.IsNullOrEmpty(paramAliasString)) continue;
                    paramAliasString = paramAliasString.ToUpperInvariant();
                    foreach (int paramAliasIndex in Enumerable.Range(0, componentDsBlock.ParamAliases.Length))
                    {
                        ref var paramAlias = ref componentDsBlock.ParamAliases[paramAliasIndex];
                        if (paramAlias.ParamAliasString == paramAliasString)
                        {
                            if (module.PrepareConnection(paramAlias.Connection, false))
                            {
                                var block = module.DsBlocksTempRuntimeData.DescendantDsBlocks[paramAlias.Connection.DsBlockIndexInModule];
                                ref var param = ref block.Params[paramAlias.Connection.ParamIndex!.Value];

                                var valueStringResolved = values[index];
                                byte connectionType = 0;
                                if (valueStringResolved is not null)
                                {
                                    StringHelper.ReplaceIgnoreCase(ref valueStringResolved, "%(TAG)", tag);
                                    connectionType = DsConnectionsFactory.GetConnectionTypeFromPrefix(ref valueStringResolved);
                                }

                                if (connectionType != 0)
                                {
                                    string? connectionString = valueStringResolved;
                                    if (String.IsNullOrEmpty(connectionString)) continue;
                                    var connection = DsConnectionsFactory.CreateConnection(connectionType, module, componentDsBlock);
                                    connection!.ConnectionString = connectionString;
                                    if (param.Connections is not null)
                                    {
                                        if (paramAlias.Connection.ParamValueIndex < param.Connections.Length)
                                            param.Connections[paramAlias.Connection.ParamValueIndex] = connection;
                                    }
                                    else
                                    {
                                        param.Connection = connection;
                                    }
                                }
                                else
                                {
                                    Any value = Any.ConvertToBestType(valueStringResolved, false);
                                    if (param.Values is not null)
                                    {
                                        if (paramAlias.Connection.ParamValueIndex < param.Values.Length)
                                            param.Values[paramAlias.Connection.ParamValueIndex].Set(value);
                                    }
                                    else
                                    {
                                        param.Value.Set(value);
                                    }
                                    if (paramAlias.Connection.IsRefToMajorParam())
                                        block.OnMajorParamsChanged();
                                }
                            }
                            break;
                        }
                    }
                }
                blocks.Add(componentDsBlock);
            }

            module.ChildDsBlocks = blocks.ToArray();            

            result.Add(module);
            return result;
        }        

        private void SaveModulesToCsv(string csvDsBlockFileFullName, IEnumerable<DsModule> modules)
        {
            var modulesArray = modules.ToArray();
            if (modulesArray.Length == 0) return;

            string template = Path.GetFileName(csvDsBlockFileFullName);
            if (!template.EndsWith(".block.csv", StringComparison.InvariantCultureIgnoreCase)) return;
            template = template.Substring(0, template.Length - ".block.csv".Length);

            var templateComponentDsBlock = _modulesTempRuntimeData.ChildDsBlocksDictionary.TryGetValue(template) as ComponentDsBlock;
            if (templateComponentDsBlock is null || !templateComponentDsBlock.IsTemplate)
            {
                templateComponentDsBlock = DsBlocksFactory.CreateStandardTemplateComponentDsBlock(template, modulesArray[0], null, template);
            }
            if (templateComponentDsBlock is null || !templateComponentDsBlock.IsTemplate)
            {
                _logger.LogError("Cannot find template block: " + csvDsBlockFileFullName);
                return;
            }

            try
            {
                using (var writer = new StreamWriter(csvDsBlockFileFullName, false, new UTF8Encoding(true)))
                {
                    var headerValues = new List<string?>();                    
                    headerValues.Add("");
                    headerValues.Add("DESC");
                    foreach (int index in Enumerable.Range(0, templateComponentDsBlock.ParamAliases.Length))
                    {
                        ref var paramAlias = ref templateComponentDsBlock.ParamAliases[index];
                        string headerValue = paramAlias.ParamAliasString;                        
                        headerValues.Add(headerValue);
                    }
                    writer.WriteLine(CsvHelper.FormatForCsv(@",", headerValues));
                    foreach (var module in modules)
                    {
                        foreach (var componentDsBlock in module.ChildDsBlocks.OfType<ComponentDsBlock>()
                            .Where(b => !b.IsTemplate && String.Equals(b.CreatedFromTemplate, template, StringComparison.InvariantCultureIgnoreCase)))
                        {
                            var values = new List<string?>();
                            values.Add(componentDsBlock.TagName);
                            values.Add(componentDsBlock.DESC.Value.ValueAsString(false));
                            foreach (int index in Enumerable.Range(0, componentDsBlock.ParamAliases.Length))
                            {
                                ref var paramAlias = ref componentDsBlock.ParamAliases[index];
                                string? value = null;                               
                                if (componentDsBlock.ParentModule.PrepareConnection(paramAlias.Connection, false))
                                {
                                    var block = componentDsBlock.ParentModule.DsBlocksTempRuntimeData.DescendantDsBlocks[paramAlias.Connection.DsBlockIndexInModule];
                                    ref var param = ref block.Params[paramAlias.Connection.ParamIndex!.Value];

                                    if (param.Connections is not null)
                                    {
                                        if (paramAlias.Connection.ParamValueIndex < param.Connections.Length)
                                        {
                                            var connection = param.Connections[paramAlias.Connection.ParamValueIndex];
                                            if (connection is not null)
                                                value = connection.ConnectionStringWithConnectionTypePrefix;
                                            else
                                                value = param.Values[paramAlias.Connection.ParamValueIndex].ValueAsString(false);
                                        }
                                    }
                                    else
                                    {
                                        var connection = param.Connection;
                                        if (connection is not null)
                                            value = connection.ConnectionStringWithConnectionTypePrefix;
                                        else
                                            value = param.Value.ValueAsString(false);
                                    }
                                }
                                if (value is not null) value = value.Replace(componentDsBlock.TagName, "%(TAG)");
                                values.Add(value);
                            }
                            writer.WriteLine(CsvHelper.FormatForCsv(@",", values));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_logger is not null)
                    _logger.LogError(ex, "Csv File Writing Error: " + csvDsBlockFileFullName);
            }            
        }

        #endregion
    }
}
