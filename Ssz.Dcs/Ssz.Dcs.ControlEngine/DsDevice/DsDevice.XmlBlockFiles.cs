using Microsoft.Extensions.Logging;
using Ssz.Utils;
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

        private List<DsModule> LoadModulesFromXml(ILogger? userFriendlyLogger, string xmlDsBlockFileFullName)
        {
            var result = new List<DsModule>();

            try
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (var fileStream = new FileStream(xmlDsBlockFileFullName, FileMode.Open, FileAccess.Read))
                    {
                        fileStream.CopyTo(memoryStream);
                    }
                    memoryStream.Position = 0;

                    using (XmlReader xmlReader = XmlReader.Create(memoryStream))
                    {
                        if (!xmlReader.ReadToFollowing(@"FileFormat")) throw new Exception("FileFormat element is not found");
                        string ver = @"";
                        while (xmlReader.MoveToNextAttribute())
                        {
                            if (xmlReader.Name == "Ver")
                            {
                                ver = xmlReader.Value;
                                break;
                            }
                        }
                        xmlReader.MoveToElement();
                        if (ver != "5.0")
                        {
                            return LoadModulesFromXmlObsolete(userFriendlyLogger, xmlDsBlockFileFullName);
                        }

                        while (xmlReader.ReadToFollowing(@"Module"))
                        {
                            DsModule? module = ImportModuleFromXml(xmlReader);
                            if (module is not null) result.Add(module);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                userFriendlyLogger?.LogError(ex.Message);
            }

            return result;
        }

        /// <summary>
        ///     xmlReader must be at Module
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <returns></returns>
        private DsModule? ImportModuleFromXml(XmlReader xmlReader)
        {
            string name = @"";
            while (xmlReader.MoveToNextAttribute())
            {
                if (xmlReader.Name == "Name")
                {
                    name = xmlReader.Value;
                    break;
                }
            }
            xmlReader.MoveToElement();
            var module = new DsModule(name, this);

            if (xmlReader.IsEmptyElement) return module;
            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == "ChildDsBlocks")
                        {
                            module.ChildDsBlocks = ImportChildDsBlocksFromXml(xmlReader, module, null);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (xmlReader.Name == "Module")
                        {
                            return module;
                        }
                        break;
                    default:
                        break;
                }
            }
            return module;
        }

        /// <summary>
        ///     xmlReader must be at ChildDsBlocks
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <returns></returns>
        private DsBlockBase[] ImportChildDsBlocksFromXml(XmlReader xmlReader, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {
            var blocks = new List<DsBlockBase>();
            if (xmlReader.IsEmptyElement) return blocks.ToArray();
            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == "DsBlock")
                        {
                            var block = ImportDsBlockFromXml(xmlReader, parentModule, parentComponentDsBlock);
                            if (block is not null) blocks.Add(block);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (xmlReader.Name == "ChildDsBlocks")
                        {
                            return blocks.ToArray();
                        }
                        break;
                    default:
                        break;
                }
            }
            return blocks.ToArray();
        }

        /// <summary>
        ///      xmlReader must be at DsBlock
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="parentModule"></param>
        /// <param name="parentComponentDsBlock"></param>
        /// <returns></returns>
        private DsBlockBase? ImportDsBlockFromXml(XmlReader xmlReader, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {
            string type = @"";
            string tag = @"";
            string shapeData = @"";
            while (xmlReader.MoveToNextAttribute())
            {
                switch (xmlReader.Name)
                {
                    case @"Type":
                        type = xmlReader.Value;
                        break;
                    case @"Tag":
                        tag = xmlReader.Value;
                        break;
                    case @"ShapeData":
                        shapeData = xmlReader.Value;
                        break;
                }
            }
            xmlReader.MoveToElement();
            DsBlockBase? block = null;
            var blockType = DsBlocksFactory.GetDsBlockType(type);
            if (blockType != 0)
            {
                block = DsBlocksFactory.CreateDsBlock(blockType, tag, parentModule, parentComponentDsBlock);
                if (block is not null)
                    block.ShapeData = shapeData;
            }

            if (xmlReader.IsEmptyElement) return block;

            var majorParamImportInfosBuffer = new List<ParamImportInfo>(255);
            var paramImportInfosBuffer = new List<ParamImportInfo>(255); 
            
            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (block is not null)
                        {
                            var componentDsBlock = block as ComponentDsBlock;
                            switch (xmlReader.Name)
                            {
                                case @"Param":
                                    ImportParamFromXml(xmlReader, block, majorParamImportInfosBuffer, paramImportInfosBuffer, parentModule, parentComponentDsBlock);
                                    break;
                                case @"ParamAliases":
                                    if (componentDsBlock is not null)
                                    {
                                        componentDsBlock.ParamAliases = ImportParamAliasesFromXml(xmlReader, componentDsBlock);
                                    }
                                    break;
                                case @"JournalParamAliases":
                                    if (componentDsBlock is not null)
                                    {
                                        componentDsBlock.JournalParamAliases = ImportJournalParamAliasesFromXml(xmlReader, componentDsBlock);
                                    }
                                    break;
                                case @"ChildDsBlocks":
                                    if (componentDsBlock is not null)
                                    {
                                        componentDsBlock.ChildDsBlocks = ImportChildDsBlocksFromXml(xmlReader, parentModule, componentDsBlock);
                                    }
                                    break;
                            }
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (xmlReader.Name == "DsBlock")
                        {
                            if (block is not null)
                            {
                                bool majorParamsChanged = false;

                                foreach (var paramValue in majorParamImportInfosBuffer)
                                {
                                    ref var param = ref block.Params[paramValue.ParamIndex];
                                    if (paramValue.ParamValueIndex != IndexConstants.ParamValueIndex_IsNotArray) // Is Array
                                    {
                                        param.Values[paramValue.ParamValueIndex] = paramValue.Value;
                                        param.Connections[paramValue.ParamValueIndex] = paramValue.Connection;
                                    }
                                    else
                                    {
                                        param.Value = paramValue.Value;
                                        param.Connection = paramValue.Connection;
                                    }
                                    majorParamsChanged = true;
                                }

                                if (majorParamsChanged) block.OnMajorParamsChanged();

                                foreach (var paramValue in paramImportInfosBuffer)
                                {
                                    ref var param = ref block.Params[paramValue.ParamIndex];
                                    if (paramValue.ParamValueIndex != IndexConstants.ParamValueIndex_IsNotArray) // Is Array
                                    {
                                        param.Values[paramValue.ParamValueIndex] = paramValue.Value;
                                        param.Connections[paramValue.ParamValueIndex] = paramValue.Connection;
                                    }
                                    else
                                    {
                                        param.Value = paramValue.Value;
                                        param.Connection = paramValue.Connection;
                                    }
                                }
                            }
                            return block;
                        }
                        break;
                    default:
                        break;
                }
            }

            return null;
        }

        /// <summary>
        ///     xmlReader must be at Param
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="block"></param>
        private void ImportParamFromXml(XmlReader xmlReader, DsBlockBase block, List<ParamImportInfo> majorParamImportInfos, List<ParamImportInfo> paramImportInfos, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {
            string name = @"";
            string connType = @"";
            string conn = @"";
            while (xmlReader.MoveToNextAttribute())
            {
                switch (xmlReader.Name)
                {
                    case @"Name":
                        name = xmlReader.Value;
                        break;
                    case @"ConnType":
                        connType = xmlReader.Value;
                        break;
                    case @"Conn":
                        conn = xmlReader.Value;
                        break;
                }
            }
            xmlReader.MoveToElement();            
            int paramIndex = block.GetParamIndex(name.ToUpperInvariant(), out byte paramValueIndex, out bool isMajor);
            if (paramIndex != IndexConstants.ParamIndex_ParamDoesNotExist)
            {
                var paramImportInfo = new ParamImportInfo { ParamIndex = paramIndex, ParamValueIndex = paramValueIndex };

                byte connectionType = DsConnectionsFactory.GetConnectionType(connType);
                if (connectionType != 0)
                {
                    DsConnectionBase? connection = DsConnectionsFactory.CreateConnection(connectionType, parentModule, parentComponentDsBlock);
                    if (connection is not null)
                    {
                        connection.ConnectionString = conn;
                        paramImportInfo.Connection = connection;                        
                    }
                }

                if (xmlReader.IsEmptyElement)
                {
                    if (isMajor)
                    {
                        majorParamImportInfos.Add(paramImportInfo);
                    }
                    else
                    {
                        paramImportInfos.Add(paramImportInfo);
                    }
                    return;
                }

                while (xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.Text:
                            paramImportInfo.Value = Any.ConvertToBestType(xmlReader.Value, false);                                                       
                            break;
                        case XmlNodeType.EndElement:
                            if (xmlReader.Name == "Param")
                            {
                                if (isMajor)
                                {
                                    majorParamImportInfos.Add(paramImportInfo);
                                }
                                else
                                {
                                    paramImportInfos.Add(paramImportInfo);
                                }
                                return;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                if (xmlReader.IsEmptyElement) return;

                while (xmlReader.Read())
                {
                    switch (xmlReader.NodeType)
                    {
                        case XmlNodeType.EndElement:
                            if (xmlReader.Name == "Param")
                            {
                                return;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        ///     xmlReader must be at ParamAliases
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="componentDsBlock"></param>
        /// <returns></returns>
        private DsParamAlias[] ImportParamAliasesFromXml(XmlReader xmlReader, ComponentDsBlock componentDsBlock)
        {
            if (xmlReader.IsEmptyElement) return new DsParamAlias[0];
            var paramAliases = new List<DsParamAlias>();
            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == "ParamAlias")
                        {
                            DsParamAlias? paramAlias = ImportParamAliasFromXml(xmlReader, componentDsBlock);
                            if (paramAlias is not null && !String.IsNullOrEmpty(paramAlias.Value.ParamAliasString))
                                paramAliases.Add(paramAlias.Value);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (xmlReader.Name == "ParamAliases")
                        {
                            return paramAliases.ToArray();
                        }
                        break;
                    default:
                        break;
                }
            }
            return new DsParamAlias[0];
        }

        /// <summary>
        ///     xmlReader must be at JournalParamAliases
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="componentDsBlock"></param>
        /// <returns></returns>
        private DsParamAlias[] ImportJournalParamAliasesFromXml(XmlReader xmlReader, ComponentDsBlock componentDsBlock)
        {
            if (xmlReader.IsEmptyElement) return new DsParamAlias[0];
            var paramAliases = new List<DsParamAlias>();
            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Element:
                        if (xmlReader.Name == "ParamAlias")
                        {
                            DsParamAlias? paramAlias = ImportParamAliasFromXml(xmlReader, componentDsBlock);
                            if (paramAlias is not null && !String.IsNullOrEmpty(paramAlias.Value.ParamAliasString))
                                paramAliases.Add(paramAlias.Value);
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (xmlReader.Name == "JournalParamAliases")
                        {
                            return paramAliases.ToArray();
                        }
                        break;
                    default:
                        break;
                }
            }
            return new DsParamAlias[0];
        }

        /// <summary>
        ///     xmlReader must be at ParamAlias
        /// </summary>
        /// <param name="xmlReader"></param>
        /// <param name="componentDsBlock"></param>
        /// <returns></returns>
        private DsParamAlias? ImportParamAliasFromXml(XmlReader xmlReader, ComponentDsBlock componentDsBlock)
        {
            string alias = @"";
            while (xmlReader.MoveToNextAttribute())
            {
                switch (xmlReader.Name)
                {
                    case @"Alias":
                        alias = xmlReader.Value;
                        break;
                }
            }
            xmlReader.MoveToElement();

            if (xmlReader.IsEmptyElement) return null;

            DsParamAlias? paramAlias = null;

            while (xmlReader.Read())
            {
                switch (xmlReader.NodeType)
                {
                    case XmlNodeType.Text:
                        if (!String.IsNullOrEmpty(alias))
                        {
                            paramAlias = new DsParamAlias(alias.ToUpperInvariant(), xmlReader.Value, componentDsBlock);
                        }                        
                        break;
                    case XmlNodeType.EndElement:
                        if (xmlReader.Name == "ParamAlias")
                        {
                            return paramAlias;
                        }
                        break;
                    default:
                        break;
                }
            }
            return paramAlias;
        }

        private void SaveModulesToXml(string xmlDsBlockFileFullName, IEnumerable<DsModule> modules)
        {
            var modulesArray = modules.ToArray();
            if (modulesArray.Length == 0) return;

            using (var fileStream = new FileStream(xmlDsBlockFileFullName, FileMode.Create, FileAccess.Write))
            using (XmlWriter writer = XmlWriter.Create(fileStream, new XmlWriterSettings
            {
                Indent = true,
            }))
            {
                writer.WriteStartElement(null, @"dataroot", null);

                writer.WriteStartElement(null, @"FileFormat", null);
                writer.WriteAttributeString(null, "Ver", null, "5.0");
                writer.WriteEndElement(); // FileFormat

                writer.WriteStartElement(null, @"Prog", null);
                writer.WriteAttributeString(null, "ID", null, "Dcs DCS Engine");
                writer.WriteAttributeString(null, "Ver", null, "1.0");
                writer.WriteEndElement(); // Prog

                writer.WriteStartElement(null, @"FileInfo", null);
                writer.WriteAttributeString(null, "User", null, "System");
                var now = new Any(DateTime.Now);
                writer.WriteAttributeString(null, "Date", null, now.ValueAsString(false, @"yyyy.MM.dd"));
                writer.WriteAttributeString(null, "Time", null, now.ValueAsString(false, @"HH:mm:ss"));
                writer.WriteEndElement(); // FileInfo                

                foreach (var module in modulesArray)
                {
                    writer.WriteStartElement(null, @"Module", null);
                    writer.WriteAttributeString(null, "Name", null, module.Name);

                    ExportChildDsBlocksToXml(writer, module.ChildDsBlocks);

                    writer.WriteEndElement(); // Module
                }

                writer.WriteEndElement(); // dataroot
                writer.Flush();
            }
        }

        private void ExportChildDsBlocksToXml(XmlWriter writer, DsBlockBase[] childDsBlocks)
        {
            writer.WriteStartElement(null, @"ChildDsBlocks", null);
            foreach (var block in childDsBlocks)
            {
                writer.WriteStartElement(null, @"DsBlock", null);
                writer.WriteAttributeString(null, "Type", null, block.DsBlockTypeString);
                writer.WriteAttributeString(null, "Tag", null, block.TagName);
                writer.WriteAttributeString(null, "ShapeData", null, block.ShapeData);                

                foreach (int index in Enumerable.Range(0, block.ParamInfos.Length))
                {
                    ExportParamToXml(writer,
                        ref block.ParamInfos[index],
                        ref block.Params[index]);
                }

                if (block is ComponentDsBlock componentDsBlock)
                {
                    writer.WriteStartElement(null, @"ParamAliases", null);
                    foreach (int index in Enumerable.Range(0, componentDsBlock.ParamAliases.Length))
                    {
                        var paramAlias = componentDsBlock.ParamAliases[index];
                        writer.WriteStartElement(null, @"ParamAlias", null);
                        writer.WriteAttributeString(null, "Alias", null, paramAlias.ParamAliasString);
                        writer.WriteString(paramAlias.Connection.ConnectionString);
                        writer.WriteEndElement(); // ParamAlias                        
                    }
                    writer.WriteEndElement(); // ParamAliases

                    writer.WriteStartElement(null, @"JournalParamAliases", null);
                    foreach (int index in Enumerable.Range(0, componentDsBlock.JournalParamAliases.Length))
                    {
                        var paramAlias = componentDsBlock.JournalParamAliases[index];
                        writer.WriteStartElement(null, @"ParamAlias", null);
                        writer.WriteAttributeString(null, "Alias", null, paramAlias.ParamAliasString);
                        writer.WriteString(paramAlias.Connection.ConnectionString);
                        writer.WriteEndElement(); // ParamAlias                        
                    }
                    writer.WriteEndElement(); // JournalParamAliases

                    ExportChildDsBlocksToXml(writer, componentDsBlock.ChildDsBlocks);
                }

                writer.WriteEndElement(); // DsBlock
            }
            writer.WriteEndElement(); // ChildDsBlocks
        }

        private void ExportParamToXml(XmlWriter writer, ref DsParamInfo paramInfo, ref DsParam param)
        {
            if (paramInfo.IsArray)
            {
                byte valuesLength = (byte)param.Values.Length;
                foreach (int index in Enumerable.Range(0, valuesLength))
                {
                    writer.WriteStartElement(null, @"Param", null);
                    writer.WriteAttributeString(null, "Name", null, paramInfo.Name + @"[" + (index + 1) + @"]");
                    var connection = param.Connections[index];
                    if (connection is not null)
                    {
                        writer.WriteAttributeString(null, "ConnType", null, connection.ConnectionTypeString);
                        writer.WriteAttributeString(null, "Conn", null, connection.ConnectionString);
                    }
                    writer.WriteString(param.Values[index].ValueAsString(false));
                    writer.WriteEndElement(); // Param
                }
            }
            else
            {
                writer.WriteStartElement(null, @"Param", null);
                writer.WriteAttributeString(null, "Name", null, paramInfo.Name);
                var connection = param.Connection;
                if (connection is not null)
                {
                    writer.WriteAttributeString(null, "ConnType", null, connection.ConnectionTypeString);
                    writer.WriteAttributeString(null, "Conn", null, connection.ConnectionString);
                }
                writer.WriteString(param.Value.ValueAsString(false));
                writer.WriteEndElement(); // Param
            }
        }

        #endregion        

        private struct ParamImportInfo
        {
            public int ParamIndex;

            public byte ParamValueIndex;

            public Any Value;

            public DsConnectionBase? Connection;
        }
    }
}
