/////////////////////////////////////////////////////////////////////////////////////////
//
//									 COPYRIGHT (c) 2018
//                                Ssz INTERNATIONAL INC.
//                            		ALL RIGHTS RESERVED
//
//  Legal rights of Ssz International Inc. in this software is distinct
//  from ownership of any medium in which the software is embodied. Copyright
//  notices must be reproduced in any copies authorized by Ssz International Inc.
//
/////////////////////////////////////////////////////////////////////////////////////////
#region Using declarations
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;
#endregion

namespace Ssz.Utils
{
    /// <summary>
    /// The HmiWebParseFile is used as a quick lookup of the important parts contained in an HMIWeb graphics file.
    /// The file is used by the search as a quick way in which to obtain search results for the graphics.
    /// The file is also used by the OPC Historian in order to determine which tag.param are needed for trending.
    /// FieldView DCS Consoles use the file to map a tagname to a faceplate
    /// </summary>
    /// <remarks><para>
    /// The HMIWeb graphics are in a known format, and we are looking for the "parameters" part of the "DIV" tag so that 
    /// we can find out what tagnames are on the graphic.  More specifically, for each of the tagnames, we want to know
    /// what faceplate is associated with the tagname, and what trends might be plotted on that faceplate.  There are a
    /// bunch of keywords that we can look for that will help us to get the information we need.  Below is an example DIV
    /// containg lots of unimportant information for us, but also containing some really good information in the "parameters"
    /// section
    /// 
    /// DIV 
    ///  tabIndex = -1 id = shape078 class=hsc.shape.1 linkType = "embedded" globalscripts = "" styleClass = "" numberOfShapesAnimated = "1" value = "1"
    ///  style="FONT-SIZE: 0px; TEXT-DECORATION: none; HEIGHT: 44px; FONT-FAMILY: Arial; WIDTH: 94px; POSITION: absolute; FONT-WEIGHT: 400; FONT-STYLE: normal; LEFT: 119px; TOP: 38px; BEHAVIOR: url(#HSCShapeLinkBehavior) url(#HSCShapeLinkBehavior#shapelinkanimator) url(#HDXVectorFactory#shapelink) url(#BindingBehavior); VISIBILITY: inherit" 
    ///  hdxproperties="fillColorBlink:False;HDXBINDINGID:1;Height:44;lineColorBlink:False;Width:94;" 
    ///  src = ".\Debutanizer_files\REGCTL_SIM2_U.sha" 
    ///  parameters = "Text?Version:USO-R300;Point?tagname:11FC01;Text?FaceplateFile:Sim2\Faceplate\PID_REGCTL_SIM2_FP_U.htm;Parameter?cp_eudesc:EUDESC;Parameter?cp_mode:MODE;Parameter?cp_pntalm:PTINAL;Parameter?cp_pntalmack:ACKSTAT;Parameter?cp_pv:PV;Parameter?cp_pvformat:PVFORMAT;Parameter?cp_sp:SP;Parameter?cp_statetxt0:STATETXT0;Parameter?cp_statetxt1:STATETXT1;Parameter?cp_statetxt2:STATETXT2;Point?AssetName:11FC01@USD;" 
    /// </para><para>
    /// 
    /// The file is in the following form
    /// <SearchFile Version="1">
    ///		<Tag TagName="11PI14" Parameters="PV SP OP" FaceplateFile="Sim2\Faceplate\PID_REGCTL_SIM2_FP_U.htm"/>
    ///		<Tag TagName="PT14" Parameters="OP" FaceplateFile="Sim2\Faceplate\DI_SIM2_FP_U.htm"/>
    ///	</SearchFile>
    ///	
    /// </para>
    /// </remarks>
    [Serializable]
    [XmlRoot("SearchFile")] //Left as "SearchFile" to handle historical version support
    public class HmiWebParseFile
    {
        #region Private Fields
        #endregion
        #region Protected Fields
        #endregion
        #region Constants
        /// <summary>
        /// Initial version of the file.  This version did not support the Parameters attribute
        /// </summary>
        public const string Version1 = "1";
        /// <summary>
        /// Version 2 of the file contains both TagName and Parameters attributes.  The Parameters attributes were added
        /// for support of the OPC Historian so that it knows which items to get trending data for.
        /// </summary>
        public const string Version2 = "2";
        /// <summary>
        /// Version 3 of the file Faceplate filename was added to help with FieldView DCS Console support and mapping
        /// of tagname->faceplate name
        /// </summary>
        public const string Version3 = "3";
        /// <summary>
        /// The search files have a .ucsparsed extension.
        /// </summary>
        public const string FileExtension = ".UcsParsed";
        /// <summary>
        /// The tag name parameter in a DIV tag in an HMIWeb file
        /// </summary>
        /// <example>
        /// parameters = "...snip... ;Point?tagname:11FC01; ...snip..." 
        /// </example>
        public const string TagnameIdentifier = "?tagname:";
        /// <summary>
        /// The asset name parameter in a DIV tag in an HMIWeb file
        /// </summary>
        /// <example>
        /// parameters = "...snip... ;Point?AssetName:11FC01@USD; ...snip..." 
        /// </example>
        public const string AssetNameIdentifier = "?AssetName:";
        /// <summary>
        /// The PV parameter parameter in a DIV tag in an HMIWeb file
        /// </summary>
        /// <example>
        /// parameters = "...snip... ;Parameter?cp_pv:PV; ...snip..." 
        /// </example>
        public const string PVIdentifier = "?cp_pv:";
        /// <summary>
        /// The SP parameter parameter in a DIV tag in an HMIWeb file
        /// </summary>
        /// <example>
        /// parameters = "...snip... ;Parameter?cp_sp:SP; ...snip..." 
        /// </example>
        public const string SPIdentifier = "?cp_sp:";
        /// <summary>
        /// The OP parameter parameter in a DIV tag in an HMIWeb file
        /// </summary>
        /// <example>
        /// parameters = "...snip... ;Parameter?cp_op:OP; ...snip..." 
        /// </example>
        public const string OPIdentifier = "?cp_op:";
        /// <summary>
        /// The faceplate file parameter in a DIV tag in an HMIWeb file
        /// </summary>
        /// <example>
        /// parameters = "...snip... ;Text?FaceplateFile:Sim2\Faceplate\PID_REGCTL_SIM2_FP_U.htm; ...snip..." 
        /// </example>
        public const string FaceplateFileIdentifier = "?FaceplateFile:";
        #endregion
        #region Constructors, Destructors, and Dispose
        /// <summary>
        /// Default Constructor
        /// </summary>
        public HmiWebParseFile()
        {
            Tags = new List<HmiWebMetaData>();
        }
        #endregion
        #region Properties
        /// <summary>
        /// The version of the file
        /// </summary>
        /// <remarks>
        /// In the XML, this is considered to be an attribute i.e. <SearchFile Version="1"/>
        /// </remarks>
        [XmlAttribute]
        public string Version { get; set; }
        /// <summary>
        /// A list of the tags
        /// </summary>
        /// <remarks>
        /// I had a hard time getting this one to show up properly in the XML.  I had to be backward compatible 
        /// with version #1 which was not generated by a C# serialization.  In version #1, the XML was as follows:
        /// <Tag TagName="MyTag1"/>
        /// <Tag TagName="MyTag2"/>
        /// 
        /// When I tried to serialize the list, it would show up as
        /// <Tags>
        ///		<Tag TagName="MyTag1"/>
        ///		<Tag TagName="MyTag2"/>
        ///	</Tags>
        ///	
        /// The key was using XmlElement.  This caused the List to be considered as an individual element rather than as a list object.
        /// </remarks>
        [XmlElement("Tag")]
        public List<HmiWebMetaData> Tags { get; set; }
        /// <summary>
        /// A helper property to retrieve just the list of tagnames
        /// </summary>
        [XmlIgnore]
        public List<string> TagNames
        {
            get
            {
                List<string> retVal = new List<string>();
                foreach (HmiWebMetaData tag in Tags)
                {
                    retVal.Add(tag.TagName);
                }
                return retVal;
            }
        }
        /// <summary>
        /// A helper property to retrieve just the list of tagnames
        /// </summary>
        [XmlIgnore]
        public List<string> TagParams
        {
            get
            {
                List<string> retVal = new List<string>();
                foreach (HmiWebMetaData tag in Tags)
                {
                    foreach (string param in tag.Parameters)
                    {
                        retVal.Add(tag.TagName + "." + param);
                    }
                }
                return retVal;
            }
        }
        #endregion
        #region Events
        #endregion
        #region Public Methods
        /// <summary>
        /// Deserializes the specified file into an instance of a HmiWebParseFile
        /// </summary>
        /// <remarks>
        /// Please note that this method does NOT handle exceptions.  Exception handling is the responsibility
        /// of the calling method.  There are many reasons why the file access could fail so make sure 
        /// that appropriate exception handlers are used.
        /// </remarks>
        /// <param name="fullFileName">The file name of the HmiWebParseFile to load</param>
        /// <returns>
        /// A HmiWebParseFile object that was deserialized from the specified file.
        /// null if the file could not be deserialized
        /// </returns>
        static public HmiWebParseFile Deserialize(string fullFileName)
        {
            HmiWebParseFile retVal = null;
            //In order to open the file as read-only, we want to open the file stream first 
            //with the access as Read and the share as Read.  Google indicates this is 
            //the safest way to open the file.
            using (FileStream fs = new FileStream(fullFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(HmiWebParseFile));
                    retVal = serializer.Deserialize(sr) as HmiWebParseFile;
                    if (retVal != null)
                    {
                        retVal.Normalize();
                    }
                }
            }

            return retVal;
        }
        /// <summary>
        /// Serializes the current object into the specified file
        /// </summary>
        /// <param name="fullFileName">The file name of the HmiWebParseFile to save to</param>
        /// <returns>
        /// An error message if the save failed.
        /// A blank string if the save succeeded
        /// </returns>
        public string Save(string fullFileName)
        {
            string errorMessage = string.Empty;
            try
            {
                //This is being saved in the current version, which is now version #3
                Version = Version3;

                //Reduce the tags/parameters to remove duplicates.
                Normalize();
                using (FileStream fs = new FileStream(fullFileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(HmiWebParseFile));
                    serializer.Serialize(sw, this);
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            return errorMessage;
        }
        #endregion
        #region Protected Methods
        #endregion
        #region Private Methods
        /// <summary>
        /// Examines the tags and merges duplicates together
        /// </summary>
        /// <remarks>
        /// This was added because Version#1 of the file would often have mixed results, such that there were two 
        /// MyTag1 objects.  One of the objects would have the PV and SP parameters and the other would have 
        /// a OP parameter.  This method will find those duplicates and "normalize" them so that there is only
        /// one instance containing all of the parameters.
        /// </remarks>
        private void Normalize()
        {
            Dictionary<string, HmiWebMetaData> normalizedTaglist = new Dictionary<string, HmiWebMetaData>();
            foreach (HmiWebMetaData tag in Tags)
            {
                HmiWebMetaData normalizedTag = null;
                string key = tag.TagName.ToLower();
                if (!normalizedTaglist.TryGetValue(key, out normalizedTag))
                {
                    normalizedTag = tag;
                    normalizedTaglist.Add(key, normalizedTag);
                }
                else
                {
                    //Add any parameters that need to be added.
                    foreach (string parameter in tag.Parameters)
                    {
                        if (!normalizedTag.Parameters.Contains(parameter))
                        {
                            normalizedTag.Parameters.Add(parameter);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(normalizedTag.FaceplatePartialFileName))
                    {
                        normalizedTag.FaceplatePartialFileName = tag.FaceplatePartialFileName;
                    }
                }

                //It seems like the Deserialize can't handle the "Parameters=""" properly.  It thinks there
                //is a blank parameter.  Remove it.
                if (normalizedTag.Parameters.Contains(""))
                {
                    normalizedTag.Parameters.Remove("");
                }
            }

            Tags = normalizedTaglist.Values.ToList();
        }
        /// <summary>
        /// Read the passed in HMIWeb graphic file (by loading into an HTML document), 
        /// and write out all the tags to a metadata ".ucsparsed" file for quick searching.
        /// The file will not be created if one already exists and is newer than the HMIWeb file
        /// </summary>
        /// <param name="fullHmiWebGraphicFileName">The full HMIWeb graphics file name</param>
        /// <param name="infoMessages">Informational messages created as part of the parsing</param>
        /// <param name="errorMessages">Error messages created as a result of failing to parse the file</param>
        /// <returns>
        /// The contents of the HMIWeb Graphics file as it has been parsed and normalized.
        /// </returns>
        static public HmiWebParseFile ParseHmiWebGraphic(string fullHmiWebGraphicFileName, out List<string> infoMessages, out List<string> errorMessages)
        {
            infoMessages = new List<string>();
            errorMessages = new List<string>();

            //If the HMIWeb graphic doesn't exist, return immediately
            if (!File.Exists(fullHmiWebGraphicFileName))
            {
                errorMessages.Add(string.Format("HMIWeb file {0} could not be opened by the search feature.", fullHmiWebGraphicFileName));
                return null;
            }

            HmiWebParseFile retVal = null;
            bool createFile = true; // Is true if the metadata file needs to be created
            string metadataFileName = fullHmiWebGraphicFileName + FileExtension; // Name of the metadata file

            //If the metadata file exists, we want to check to see if we need to re-create it or not.
            //We will re-create it if the the metadata file is older than the HMIWeb file
            if (File.Exists(metadataFileName))
            {
                //File exists, we don't have to create it.
                createFile = File.GetLastWriteTime(fullHmiWebGraphicFileName) > File.GetLastWriteTime(metadataFileName);

                if (!createFile)
                {
                    try
                    {
                        //Great.  We can save time by just reading in the existing metadata file.
                        retVal = HmiWebParseFile.Deserialize(metadataFileName);

                        if (retVal != null && retVal.Version != HmiWebParseFile.Version3)
                        {
                            //Whoops - we need to upgrade the file.  It was created with an older
                            //version of the parser.
                            retVal = null; //Setting to null will trigger a CreateFile = true below.
                        }
                    }
                    catch (Exception ex)
                    {
                        //Failed to load the file.  We'll log the message and then indicate that we need 
                        //to re-create the file since it is mostly likely in an unknown format.
                        infoMessages.Add(string.Format("Failed to load '{0}'.  The file will be re-created\n\n{1}", metadataFileName, ex.Message));
                        retVal = null;  //Setting to null will trigger a CreateFile = true below.
                    }
                    finally
                    {
                        //If there was some problem loading the search file, we'll re-create it.
                        if (retVal == null)
                        {
                            createFile = true;
                        }
                    }
                }
            }

            //If things have gone well, we have read in an existing metdata data file which will save us a lot of
            //time.  But if the metadata file didn't exist, or there was some other error, we'll have to take 
            //the more time consuming route and parse the HMIWeb graphic file.
            if (createFile)
            {
                try
                {
                    //Read the entire HMIWeb file into memory
                    string fileContents = File.ReadAllText(fullHmiWebGraphicFileName);

                    //Put the text into an in-memory browser so that we can parse it.
                    using (WebBrowser browser = new WebBrowser())
                    {
                        browser.ScriptErrorsSuppressed = true;
                        browser.DocumentText = fileContents;
                        browser.Document.OpenNew(true);
                        browser.Document.Write(fileContents);
                        browser.Refresh();

                        HtmlDocument pHtmlDoc3 = browser.Document;

                        //Create the HmiWebParseFile object that we will populate.
                        retVal = new HmiWebParseFile();

                        //We are looking for something like the following ...
                        //<DIV 
                        //  tabIndex = -1 id = shape078 class=hsc.shape.1 linkType = "embedded" globalscripts = "" styleClass = "" numberOfShapesAnimated = "1" value = "1"
                        //  style="FONT-SIZE: 0px; TEXT-DECORATION: none; HEIGHT: 44px; FONT-FAMILY: Arial; WIDTH: 94px; POSITION: absolute; FONT-WEIGHT: 400; FONT-STYLE: normal; LEFT: 119px; TOP: 38px; BEHAVIOR: url(#HSCShapeLinkBehavior) url(#HSCShapeLinkBehavior#shapelinkanimator) url(#HDXVectorFactory#shapelink) url(#BindingBehavior); VISIBILITY: inherit" 
                        //  hdxproperties="fillColorBlink:False;HDXBINDINGID:1;Height:44;lineColorBlink:False;Width:94;" 
                        //  src = ".\Debutanizer_files\REGCTL_SIM2_U.sha" 
                        //  parameters = "Text?Version:USO-R300;Point?tagname:11FC01;Text?FaceplateFile:Sim2\Faceplate\PID_REGCTL_SIM2_FP_U.htm;Parameter?cp_eudesc:EUDESC;Parameter?cp_mode:MODE;Parameter?cp_pntalm:PTINAL;Parameter?cp_pntalmack:ACKSTAT;Parameter?cp_pv:PV;Parameter?cp_pvformat:PVFORMAT;Parameter?cp_sp:SP;Parameter?cp_statetxt0:STATETXT0;Parameter?cp_statetxt1:STATETXT1;Parameter?cp_statetxt2:STATETXT2;Point?AssetName:11FC01@USD;" 
                        // >
                        //Get all DIV tags from the HTML document
                        HtmlElementCollection pDivCollection = pHtmlDoc3.GetElementsByTagName("DIV");
                        foreach (HtmlElement pDivElement in pDivCollection)
                        {
                            // Now get the "parameters" attribute
                            string attributeValue = pDivElement.GetAttribute("parameters");

                            // Now find the "tagname" and "assetName" parameters and create XML element for each (if found)
                            // Will be of the format ?tagname:11HC01; or ?AssetName:11HC01;
                            string tagName = GetParameterValue(attributeValue, TagnameIdentifier);
                            if (!string.IsNullOrWhiteSpace(tagName))
                            {
                                HmiWebMetaData tagInfo = new HmiWebMetaData(tagName);

                                //We have a tagname.  If we have a faceplate, then we will potentially have a 
                                //trendable point ... which means that we want to search for the trendable parameters
                                string faceplateFile = GetParameterValue(attributeValue, FaceplateFileIdentifier);
                                if (!string.IsNullOrWhiteSpace(faceplateFile))
                                {
                                    //Store off the "short" faceplate name associated with this tag
                                    tagInfo.FaceplatePartialFileName = faceplateFile;

                                    bool foundPv = false;
                                    bool foundSp = false;

                                    //Look for the PV, SP, and OP parameters
                                    string pv = GetParameterValue(attributeValue, PVIdentifier);
                                    if (!string.IsNullOrWhiteSpace(pv))
                                    {
                                        tagInfo.Parameters.Add(pv);
                                        foundPv = true;
                                    }

                                    string sp = GetParameterValue(attributeValue, SPIdentifier);
                                    if (!string.IsNullOrWhiteSpace(sp))
                                    {
                                        tagInfo.Parameters.Add(sp);
                                        foundSp = true;
                                    }

                                    string op = GetParameterValue(attributeValue, OPIdentifier);
                                    if (!string.IsNullOrWhiteSpace(op))
                                    {
                                        tagInfo.Parameters.Add(op);
                                    }
                                    else if (foundPv && foundSp)
                                    {
                                        //We are looking for trendable parameters.  In many cases, the item on the main graphic
                                        //will display both PV and SP to the user.  If the user clicks on the icon, it will
                                        //bring up a faceplate containing trending information for PV, SP, and OP.  So if we 
                                        //find something with both PV and SP, we'll automatically add OP.
                                        tagInfo.Parameters.Add("OP");
                                    }
                                }

                                retVal.Tags.Add(tagInfo);
                            }

                            string assetName = GetParameterValue(attributeValue, AssetNameIdentifier);
                            if (!string.IsNullOrWhiteSpace(assetName))
                            {
                                retVal.Tags.Add(new HmiWebMetaData(assetName));
                            }
                        }

                        //Write the XML file & do cleanup ... but only if we have some valid information
                        if (retVal.Tags.Count > 0)
                        {
                            string saveError = retVal.Save(metadataFileName);
                            if (!string.IsNullOrEmpty(saveError))
                            {
                                errorMessages.Add(saveError);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    errorMessages.Add(string.Format("Error:\nFailed to parse the HMIWeb file '{0}'\nConsequences:\nSearch results for this graphic will not be available.", fullHmiWebGraphicFileName));
                }
            }

            return retVal;
        }
        /// <summary>
        /// Retrieve the value of the given parameter (of format ?xyz:) from the 
        /// given parameter string.
        /// </summary>
        /// <remarks>
        /// The parameters string is expected to be in the following format elementType?keyword:value.  
        /// Note that the various elementType/parameter pair is separated by a '?' and terminated with 
        /// a ';'.  Within the parameter, there is a keyword/value pair separated by a ':'.  So if we 
        /// are looking for a tagname, we will be looking for ?tagname: and then we want anything following
        /// the ':' up until the terminating ';'
        /// 
        /// Here is an example pulled from an HMIWeb graphic ...
        /// parameters = "Text?Version:USO-R300;Point?tagname:11FC01;Text?FaceplateFile:Sim2\Faceplate\PID_REGCTL_SIM2_FP_U.htm;
        ///               Parameter?cp_eudesc:EUDESC;Parameter?cp_mode:MODE;Parameter?cp_pntalm:PTINAL;Parameter?cp_pntalmack:ACKSTAT;
        ///               Parameter?cp_pv:PV;Parameter?cp_pvformat:PVFORMAT;Parameter?cp_sp:SP;Parameter?cp_statetxt0:STATETXT0;
        ///               Parameter?cp_statetxt1:STATETXT1;Parameter?cp_statetxt2:STATETXT2;Point?AssetName:11FC01@USD;" 
        /// </remarks>
        /// <param name="parametersString">The parameters string from HMIWeb containing all of the parameters for this DIV</param>
        /// <param name="keywordToFind">The parameter to find (of format ?xyz:)</param>
        /// <returns>
        /// The value of the parameter or an empty string if not found.
        /// </returns>
        static string GetParameterValue(string parametersString, string keywordToFind)
        {
            string retVal = string.Empty;
            int keywordLength = keywordToFind.Length;

            // Convert parameters string & parameter to find to upper case to avoid any case-sensitive
            // issues (FSC files use parameter "TagName" and other HMIWeb files use "tagname".
            int foundIndex = parametersString.ToUpper().IndexOf(keywordToFind.ToUpper());
            int valueStartIndex = foundIndex + keywordLength;
            if (foundIndex > 0 && valueStartIndex < parametersString.Length)
            {
                //We should have something in the form "?tagname:11HS14A;Text?FaceplateFile:...etc..."
                //Get the string starting after the ':' of the keyword 
                int valueEndIndex = parametersString.IndexOf(';', valueStartIndex);
                if (valueEndIndex < parametersString.Length)
                {
                    //Get the string between the ':' and ';'.  This is the 'value' described above in the remarks section
                    retVal = parametersString.Substring(valueStartIndex, valueEndIndex - valueStartIndex);
                }
            }

            return retVal;
        }

        #endregion
    }
    /// <summary>
    /// A Utility class to support the <seealso cref="HmiWebParseFile "/> class.
    /// This is essentially an encapsulation of each line in the parse file
    /// </summary>
    [Serializable]
    public class HmiWebMetaData
    {
        /// <summary>
        /// Default Constructor required for serialization
        /// </summary>
        public HmiWebMetaData()
        {
        }
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="tagName">The tagname</param>
        public HmiWebMetaData(string tagName)
        {
            TagName = tagName;
        }
        /// <summary>
        /// The tag name that was found in the parse file
        /// </summary>
        [XmlAttribute]  //Added to support .search file serialization
        public string TagName { get; set; } = string.Empty;
        /// <summary>
        /// A list of Parameters associated with this tag
        /// </summary>
        [XmlAttribute]  //Added to support .search file serialization
        public List<string> Parameters { get; set; } = new List<string>();
        /// <summary>
        /// The faceplate file name associated with this tagname
        /// </summary>
        [XmlAttribute]  //Added to support .search file serialization
        public string FaceplatePartialFileName { get; set; }
    }
}
