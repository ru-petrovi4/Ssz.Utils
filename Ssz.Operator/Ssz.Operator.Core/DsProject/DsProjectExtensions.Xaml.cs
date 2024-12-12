using System;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core
{
    public static partial class DsProjectExtensions
    {
        #region public functions

        public static void ExportObjectToXaml(this DsProject dsProject, object obj)
        {
            if (obj is null) return;
            if (!dsProject.IsInitialized) return;

            var dlg = new SaveFileDialog
            {
                Title = Resources.ExportToXamlSaveAsDialogTitle,
                Filter = @"Save file (*.xaml)|*.xaml|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return;

            string xamlFileName = dlg.FileName;

            using (
                var xmlTextWriter =
                    new XmlTextWriter(
                        File.Create(xamlFileName),
                        Encoding.UTF8)
            )
            {
                xmlTextWriter.Formatting =
                    Formatting.Indented;
                xmlTextWriter.WriteStartDocument();
                xmlTextWriter.WriteStartElement(
                    "DsProject");

                XamlHelper.Save(obj,
                    xmlTextWriter);

                xmlTextWriter.WriteEndElement();
                xmlTextWriter.WriteEndDocument();
                xmlTextWriter.Close();
            }
        }


        public static object? ImportObjectFromXaml(this DsProject dsProject)
        {
            if (!dsProject.IsInitialized) return false;

            var dlg = new OpenFileDialog
            {
                Title = Resources.ImportFromXamlDialogTitle,
                Filter = @"Open file (*.xaml)|*.xaml|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() != true) return null;

            string xamlFileName = dlg.FileName;

            using (
                var xmlTextReader =
                    new XmlTextReader(
                        File.OpenRead(xamlFileName)))
            {
                xmlTextReader.MoveToContent();
                if (xmlTextReader.NodeType ==
                    XmlNodeType.EndElement)
                    return false;
                if (xmlTextReader.NodeType !=
                    XmlNodeType.Element) return false;
                if (xmlTextReader.Name !=
                    "DsProject") return false;
                while (xmlTextReader.Read())
                {
                    if (xmlTextReader.NodeType ==
                        XmlNodeType.EndElement)
                        break;
                    if (xmlTextReader.NodeType !=
                        XmlNodeType.Element)
                        continue;

                    try
                    {
                        string objectXml =
                            xmlTextReader.ReadOuterXml();
                        var obj = XamlHelper.Load(objectXml);
                        return obj;
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogError(ex.Message);
                        return null;
                    }
                }
            }

            return null;
        }

        #endregion
    }
}