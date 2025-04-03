using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using Avalonia.Media;
using Ssz.Operator.Core;
using Ssz.Operator.Core.DsShapes.Trends;

namespace Ssz.Operator.Core.ControlsCommon.Trends.GenericTrends
{
    public class TrendsGroupConfiguration
    {
        #region public functions

        public bool IsNew { get; set; }

        public bool IsChanged { get; set; }

        public Color? PlotAreaBackgroundColor { get; set; }
        public Color? PlotBackgroundColor { get; set; }

        public DsTrendItem[] DsTrendItemsCollection { get; set; } = null!;

        #endregion
    }

    public class TrendsConfiguration
    {
        #region public functions

        /// <summary>
        ///     Main trend window Collection
        ///     != null
        /// </summary>
        public List<TrendsGroupConfiguration> TrendsGroupConfigurationsCollection { get; set; } = new List<TrendsGroupConfiguration>();

        public static TrendsConfiguration FromFile(string fullPathToFile)
        {
            var trendsConfiguration = new TrendsConfiguration();

            if (File.Exists(fullPathToFile))
            {
                XDocument document = XDocument.Load(fullPathToFile);

                foreach (XElement xmlTrendGroupConfiguration in
                    document.XPathSelectElements("/root/MainTrendWindow/Configuration"))
                {
                    TrendsGroupConfiguration trendGroupConfiguration =
                        LoadTrendGroupConfigurationFromXml(xmlTrendGroupConfiguration);

                    trendsConfiguration.TrendsGroupConfigurationsCollection.Add(trendGroupConfiguration);
                }

                //foreach (XElement xmlTrendGroupConfiguration in 
                //    document.XPathSelectElements("/root/TrendGroupsConfiguration/Configuration"))
                //{
                //    XAttribute xmlGroupId = xmlTrendGroupConfiguration.Attribute("GroupId");
                //    int groupId;
                //    if (xmlGroupId == null || !int.TryParse(xmlGroupId.Value, out groupId))
                //        continue;

                //    TrendsGroupConfiguration trendGroupConfiguration =
                //        LoadTrendGroupConfigurationFromXml(xmlTrendGroupConfiguration);

                //    trendsConfiguration._trendGroupsConfiguration.Add(
                //        new Tuple<int, TrendsGroupConfiguration>(groupId, trendGroupConfiguration));
                //}

                //foreach (XElement xmlTrendGroupConfiguration in
                //    document.XPathSelectElements("/root/SingleTrendsConfiguration/Configuration"))
                //{
                //    XAttribute xmlTag = xmlTrendGroupConfiguration.Attribute("Tag");
                //    if (xmlTag == null)
                //        continue;

                //    string tag = xmlTag.Value;
                //    if (string.IsNullOrEmpty(tag))
                //        continue;

                //    TrendsGroupConfiguration trendGroupConfiguration =
                //        LoadTrendGroupConfigurationFromXml(xmlTrendGroupConfiguration);

                //    trendsConfiguration._singleTrendsConfiguration.Add(
                //        new Tuple<string, TrendsGroupConfiguration>(tag, trendGroupConfiguration));
                //}
            }

            return trendsConfiguration;
        }

        public void Save(string fullPathToFile)
        {
            var xmlDocument = new XDocument();

            var xmlRoot = new XElement("root");
            xmlDocument.Add(xmlRoot);

            if (TrendsGroupConfigurationsCollection.Count > 0)
            {
                var xmlTrendGroupsConfiguration = new XElement("MainTrendWindow");
                xmlRoot.Add(xmlTrendGroupsConfiguration);

                foreach (var trendGroupConfiguration in TrendsGroupConfigurationsCollection)
                {
                    XElement xmlConfiguration = SaveTrendGroupConfigurationToXml(trendGroupConfiguration);                    
                    xmlTrendGroupsConfiguration.Add(xmlConfiguration);
                }
            }

            //if (_trendGroupsConfiguration.Count > 0)
            //{
            //    var xmlTrendGroupsConfiguration = new XElement("TrendGroupsConfiguration");
            //    xmlRoot.Add(xmlTrendGroupsConfiguration);

            //    foreach (var trendGroupConfiguration in _trendGroupsConfiguration)
            //    {
            //        XElement xmlConfiguration = SaveTrendGroupConfigurationToXml(trendGroupConfiguration.Item2);

            //        xmlConfiguration.SetAttributeValue("GroupId", trendGroupConfiguration.Item1);
            //        xmlTrendGroupsConfiguration.Add(xmlConfiguration);
            //    }
            //}

            //if (_singleTrendsConfiguration.Count > 0)
            //{
            //    var xmlSingleTrendsConfiguration = new XElement("SingleTrendsConfiguration");
            //    xmlRoot.Add(xmlSingleTrendsConfiguration);

            //    foreach (var singleTrendConfiguration in _singleTrendsConfiguration)
            //    {
            //        XElement xmlConfiguration = SaveTrendGroupConfigurationToXml(singleTrendConfiguration.Item2);

            //        xmlConfiguration.SetAttributeValue("Tag", singleTrendConfiguration.Item1);
            //        xmlSingleTrendsConfiguration.Add(xmlConfiguration);
            //    }
            //}

            xmlDocument.Save(fullPathToFile);
        }

        public TrendsGroupConfiguration GetTrendGroupConfiguration(int groupId)
        {
            Tuple<int, TrendsGroupConfiguration>? config =
                _trendGroupsConfiguration.FirstOrDefault(i => i.Item1 == groupId);
            if (config == null)
            {
                config = new Tuple<int, TrendsGroupConfiguration>(groupId, new TrendsGroupConfiguration {IsNew = true});
                _trendGroupsConfiguration.Add(config);
            }

            return config.Item2;
        }

        public TrendsGroupConfiguration GetSingleTrendConfiguration(string tagName)
        {
            Tuple<string, TrendsGroupConfiguration>? config =
                _singleTrendsConfiguration.FirstOrDefault(i => i.Item1 == tagName);
            if (config == null)
            {
                config = new Tuple<string, TrendsGroupConfiguration>(tagName,
                    new TrendsGroupConfiguration {IsNew = true});
                _singleTrendsConfiguration.Add(config);
            }

            return config.Item2;
        }

        #endregion

        #region private functions

        private static XElement SaveTrendGroupConfigurationToXml(TrendsGroupConfiguration trendsGroupConfiguration)
        {
            var xmlConfiguration = new XElement("Configuration");

            if (trendsGroupConfiguration.PlotAreaBackgroundColor != null)
                xmlConfiguration.Add(new XElement("PlotAreaBackground", trendsGroupConfiguration.PlotAreaBackgroundColor));
            if (trendsGroupConfiguration.PlotBackgroundColor != null)
                xmlConfiguration.Add(new XElement("PlotBackground", trendsGroupConfiguration.PlotBackgroundColor));

            if (trendsGroupConfiguration.DsTrendItemsCollection != null)
            {
                foreach (DsTrendItem trendItemInfo in trendsGroupConfiguration.DsTrendItemsCollection)
                {
                    var trendItemInfoString = XamlHelper.Save(trendItemInfo);
                    XElement trendItemInfoXElement = XElement.Parse(trendItemInfoString);
                    xmlConfiguration.Add(trendItemInfoXElement);
                }
            }

            return xmlConfiguration;
        }

        private static TrendsGroupConfiguration LoadTrendGroupConfigurationFromXml(XElement xmlTrendGroupConfiguration)
        {
            var trendsGroupConfiguration = new TrendsGroupConfiguration();

            Color? plotAreaBackgroundColor = ToColor(xmlTrendGroupConfiguration.Element("PlotAreaBackground"));
            if (plotAreaBackgroundColor != null)
                trendsGroupConfiguration.PlotAreaBackgroundColor = plotAreaBackgroundColor;

            Color? plotBackgroundColor = ToColor(xmlTrendGroupConfiguration.Element("PlotBackground"));
            if (plotBackgroundColor != null)
                trendsGroupConfiguration.PlotBackgroundColor = plotBackgroundColor;

            var trendItemInfosCollection = new List<DsTrendItem>();
            foreach (XElement trendItemInfoXElement in
                xmlTrendGroupConfiguration.XPathSelectElements("*[local-name()='DsTrendItem']"))
            {
                try
                {
                    var trendItemInfo = (DsTrendItem)XamlHelper.Load(trendItemInfoXElement.ToString(SaveOptions.None))!;
                    trendItemInfosCollection.Add(trendItemInfo);
                }
                catch (Exception)
                {
                }
            }

            trendsGroupConfiguration.DsTrendItemsCollection = trendItemInfosCollection.ToArray();

            return trendsGroupConfiguration;
        }

        private static Color? ToColor(XElement? element)
        {
            if (element == null) 
                return null;

            System.Drawing.Color systemDrawingColor = System.Drawing.ColorTranslator.FromHtml(element.Value);

            return Color.FromArgb(
                systemDrawingColor.A,
                systemDrawingColor.R,
                systemDrawingColor.G,
                systemDrawingColor.B);
        }

        #endregion

        #region private fields

        private readonly List<Tuple<int, TrendsGroupConfiguration>> _trendGroupsConfiguration =
            new List<Tuple<int, TrendsGroupConfiguration>>();

        private readonly List<Tuple<string, TrendsGroupConfiguration>> _singleTrendsConfiguration =
            new List<Tuple<string, TrendsGroupConfiguration>>();

        #endregion
    }
}