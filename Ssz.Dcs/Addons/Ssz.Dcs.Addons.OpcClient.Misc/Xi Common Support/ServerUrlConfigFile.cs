/**********************************************************************
 * Copyright © 2009, 2010, 2011, 2012 OPC Foundation, Inc. 
 *
 * The source code and all binaries built with the OPC .NET 3.0 source
 * code are subject to the terms of the Express Interface Public
 * License (Xi-PL).  See http://www.opcfoundation.org/License/Xi-PL/
 *
 * The source code may be distributed from an OPC member company in
 * its original or modified form to its customers and to any others who
 * have software that needs to interoperate with the OPC member's OPC
* .NET 3.0 products. No other redistribution is permitted.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *********************************************************************/

using System;
using System.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.IO;


namespace Xi.Common.Support
{
	public class ServerUrlConfigFile : IDisposable
	{
		#region Public Members
		/// <summary>
		/// This method is the standard Dispose() method
		/// </summary>
		public void Dispose()
		{
			if (Dispose(true))
				GC.SuppressFinalize(this);
		}

		/// <summary>
		/// This method is the standard destructor
		/// </summary>
		~ServerUrlConfigFile()
		{
			Dispose(false);
		}

		/// <summary>
		/// This method disposes of this object if requested
		/// </summary>
		/// <param name="isDisposing">Indicates when TRUE that the 
		/// object is to be disposed</param>
		/// <returns>Returns true to indicate the object was disposed.</returns>
		protected virtual bool Dispose(bool isDisposing)
		{
			if (isDisposing)
				CloseFile();
			return true;
		}

		/// <summary>
		/// This property hold the full path of the manual 
		/// configuration file for Xi Servers.  This is derived from 
		/// the .config file for the Discovery Server .exe.  The key 
		/// is "ServerIPAddressFile" and the value may contain 
		/// environment variables quoted with '%'.  This allows for 
		/// the manual configuration of a list of Xi Servers.
		/// </summary>
		internal string _FilePath;
		public string FilePath 
		{
			get
			{
				if (string.IsNullOrEmpty(_FilePath))
				{
					try
					{
						string serverAddressFilePath = ConfigurationManager.AppSettings["ServerUrlFilePath"];
						if (null != serverAddressFilePath && 0 < serverAddressFilePath.Length)
						{
							_FilePath = Environment.ExpandEnvironmentVariables(serverAddressFilePath);
							// remove the end slash from the path if was entered
							char[] slashes = new char[] { '/', '\\' };
							_FilePath = _FilePath.TrimEnd(slashes);
						}
					}
					catch
					{
					}
				}
				return _FilePath;
			}
		}

		/// <summary>
		/// This property hold the full name (with path) of the manual 
		/// configuration file for Xi Servers.  This is derived from 
		/// the .config file for the Discovery Server .exe.  The key 
		/// is "ServerIPAddressFile" and the value may contain 
		/// environment variables quoted with '%'.  This allows for 
		/// the manual configuration of a list of Xi Servers.
		/// </summary>
		internal string _FileName;
		public string FileName 
		{ 
			get 
			{
				try
				{
					string serverAddressFileName = ConfigurationManager.AppSettings["ServerUrlFileName"];
					if (null != serverAddressFileName && 0 < serverAddressFileName.Length)
					{
						_FileName = FilePath + "\\" + serverAddressFileName;
					}
				}
				catch
				{
				}
				return _FileName;
			} 
		}

		public static IEnumerable<string> GetServerUrls()
		{
			ServerUrlConfigFile file = new ServerUrlConfigFile();
			file.OpenFile();
			List<string> allUrls;
			List<string> configuredUrls;
			List<string> pnrpUrls;
			file.GetUrlsFromFile(out allUrls, out configuredUrls, out pnrpUrls);
			file.Dispose();
			return allUrls;
		}

		#endregion // Public Members

		#region Non-Public Members
		/// <summary>
		/// Indicates if the PNRP Section of the Manual Configuration File 
		/// is present.
		/// </summary>
		protected bool _PnrpSectionPresent = false;

		/// <summary>
		/// Specifies the header line for the PNRP Section of the Manual Configuration File
		/// </summary>
		protected static string _PnrpSectionHeader = "# [PNRP Section]";

		/// <summary>
		/// The Manual Configuration File
		/// </summary>
		protected FileStream _FileStream;

		/// <summary>
		/// The Manual Configuration File Reader
		/// </summary>
		protected StreamReader _StreamReader;

		/// <summary>
		/// The Manual Configuration File Reader
		/// </summary>
		protected StreamWriter _StreamWriter;

		/// <summary>
		/// This method creates the directory and manual configuration file if necessary
		/// </summary>
		protected void OpenFile()
		{
			if (_FileStream != null)
				CloseFile();

			if ((null != FilePath)
				&& (null != FileName))
			{
				// Determine whether the directory for the config file exists and create it if it doesn't.
				if (!Directory.Exists(FilePath))
				{
					try { Directory.CreateDirectory(FilePath); }
					catch
					{
						// TODO: Customize how this is logged/reported
						// Write to either the console or the event log
						// WriteLine("The directory for the Xi Discovery Server's manual configuration file could not be created.  Directory name = " + ServerRoot.DiscoveryServerManualConfigPath);
					}
				}

				// the directory exists, so try to read the file
				bool fileExists = false;
				try
				{
					fileExists = File.Exists(_FileName);
				}
				catch { }

				if (fileExists)
				{
					for (int count = 0; count < 10; count++)
					{
						try
						{
							// determine if the PNRP Section header is present
							_FileStream = new FileStream(
									_FileName,
									FileMode.Open,
									FileAccess.ReadWrite,
									FileShare.None);

							_StreamReader = new StreamReader(_FileStream);
							while (!_StreamReader.EndOfStream)
							{
								string line = _StreamReader.ReadLine().Trim();
								if (!string.IsNullOrEmpty(line))
								{
									if (_PnrpSectionHeader == line)
									{
										_PnrpSectionPresent = true;
										break;
									}
								}
							}
							break;
						}
						catch (IOException)
						{
							if (_FileStream != null)
							{
								CloseFile();
							}
							Thread.Sleep(1000);
						}
						catch (Exception)
						{
							if (_FileStream != null)
							{
								CloseFile();
								break;
							}
						}
					}
				}
				else // file doesn't exist
				{
					_PnrpSectionPresent = false;
					// create the file if it does not exist
					try
					{
						// determine if the PNRP Section header is present
						_FileStream = new FileStream(
								_FileName,
								FileMode.Create,
								FileAccess.ReadWrite,
								FileShare.None);

						_StreamWriter = new StreamWriter(_FileStream);
						_StreamWriter.WriteLine("# This file contains the manually configured URLs or the IP addresses:port numbers");
						_StreamWriter.WriteLine("# of Xi Servers. IP addresses:port numbers are used when the Discovery URL of the");
						_StreamWriter.WriteLine("# server uses http and the default service name of \"XiServices/serverDiscovery\"");
						_StreamWriter.WriteLine("#");
						_StreamWriter.WriteLine("# This file also contains URLs of servers discovered using PNRP (if any).");
						_StreamWriter.WriteLine("# The PNRP section of the file is automatically created and Server URLs are dynamically");
						_StreamWriter.WriteLine("# added and deleted by the XI Discovery Server. ");
						_StreamWriter.WriteLine("#");
						_StreamWriter.WriteLine("# To prevent a Server URL from being automatically deleted when the server is no longer");
						_StreamWriter.WriteLine("# detected by PNRP (normally when it is stopped), move it above the PNRP Section header.");
						_StreamWriter.WriteLine("#");
						_StreamWriter.WriteLine("# Each line in this file is either a URL, a port address, or a comment line.");
						_StreamWriter.WriteLine("#");
						_StreamWriter.WriteLine("# URL lines contain a full URL, such as");
						_StreamWriter.WriteLine("# HTTP://MYMACHINE:58090/XISERVICES/SERVERDISCOVERY");
						_StreamWriter.WriteLine("#");
						_StreamWriter.WriteLine("# Port address lines contain an IP4 Address and port separated by a colon, such as");
						_StreamWriter.WriteLine("# 10.210.44.108:58080");
						_StreamWriter.WriteLine("#");
						_StreamWriter.WriteLine("# Comment lines start with the # character.");
						_StreamWriter.WriteLine("#");
						_StreamWriter.WriteLine("# [Manual Configuration Section]");

						// Open the Reader for subsequent access
						_StreamReader = new StreamReader(_FileStream);
					}
					catch
					{
						if (_FileStream != null)
						{
							CloseFile();
						}
						// TODO: Customize how this is logged/reported
						// Write to either the console or the event log
						// WriteLine("The Xi Discovery Server's manual configuration file could not be created.  File name = " + ServerRoot.DiscoveryServerManualConfigFile);
					}
				}
			}
		}

		protected void ResetStreamToBeginning()
		{
			_StreamReader.ReadToEnd();
			_StreamReader.BaseStream.Position = 0;
		}

		/// <summary>
		/// This method creates the directory and manual configuration file if necessary
		/// </summary>
		protected void CloseFile()
		{
			if (_StreamWriter != null)
			{
				_StreamWriter.Close();
				_StreamWriter = null;
			}
			if (_StreamReader != null)
			{
				_StreamReader.Close();
				_StreamReader = null;
			}
			if (_FileStream != null)
			{
				_FileStream.Close();
				_FileStream = null;
			}
		}

		/// <summary>
		/// Retrieves the list of URLs from the Manual Configuration File. Then determines 
		/// which are new and which were deleted (which were in the previous list but not 
		/// in the current list).
		/// </summary>
		/// <returns>A new manual configuration section if there were errors in this section</returns>
		protected List<string> GetUrlsFromFile(out List<string> allFileUrls, out List<string> configuredUrls, out List<string> pnrpUrls)
		{
			List<string> manConfigSection = new List<string>();
			allFileUrls = new List<string>();
			configuredUrls = new List<string>();
			pnrpUrls = new List<string>();
			List<string> currentFileUrls = new List<string>();
			bool errorDetected = false;
			if (_FileStream != null)
			{
				try
				{
					ResetStreamToBeginning();
					bool pnrpSection = false;
					while (!_StreamReader.EndOfStream)
					{
						string line = _StreamReader.ReadLine().Trim();
						if (!string.IsNullOrEmpty(line))
						{
							// break after all the manually configured URLs
							if (string.Compare(line, _PnrpSectionHeader) == 0)
								pnrpSection = true;
							else if ('#' != line[0]) // if this is not a comment line
							{
								// The ServerIPAddressFile.txt file contains a list of IP addresses and 
								// full URLs for Xi Servers.  Each IP address or URL must be entered on a 
								// single line.  Lines begriming with a "#" are considered comment lines 
								// and are ignored.
								// If an error was detected for a URL line, then the line is replaced with
								// an error comment line followed by the URL line preceded by a "#"
								// otherwise the original line is returned in "lines".
								string url = null;
								List<string> lines = ConvertFileLineToUrl(line, out url);

								// save the manual config section lines in case an error is detected
								// and it has to be rewritten
								if (pnrpSection == false)
								{
									if (lines.Count > 1)
										errorDetected = true;
									manConfigSection.AddRange(lines);
								}

								if (string.IsNullOrEmpty(url) == false)
								{
									allFileUrls.Add(url);
									if (pnrpSection)
										pnrpUrls.Add(url);
									else
										configuredUrls.Add(url);
								}
							}
							else
							{
								// save the manual config section line in case an error is detected
								// and it has to be rewritten
								if (pnrpSection == false)
									manConfigSection.Add(line);
							}
						}
						else
						{
							// save the manual config section line in case an error is detected
							// and it has to be rewritten
							if (pnrpSection == false)
								manConfigSection.Add(line);
						}
					} // end while
				}
				catch { }
			}
			if (errorDetected == false)
				manConfigSection = null;
			return manConfigSection;
		}

		/// <summary>
		/// This method converts a line in a file to a URL if possible
		/// </summary>
		/// <param name="line">The line to convert</param>
		/// <returns>The original line if it is a comment or a valid URL line. Otherwise the 
		/// input line contains an invalid URL and two lines are returned. The first contains 
		/// a comment line that indicates the URL is in error and the second line contains the 
		/// URL preceded by the "#" comment indicator.</returns>
		private List<string> ConvertFileLineToUrl(string line, out string outUrl)
		{
			List<string> returnLines = new List<string>();
			outUrl = null;
			UriBuilder ub = null;
			// see if it is hostname or ip address plus a port number
			// check for a slash which is required 
			int slashPos = line.LastIndexOf('/');
			if (slashPos == -1) // no slash, so not a URL
			{
				int colonPos = line.LastIndexOf(':');
				if ((colonPos > -1) // colon not present
					&& (colonPos != 0) // colon is not the first character => host present
					&& (colonPos != line.Length - 1) // colon is not the last character => port present
				   )
				{
					string hostStr = line.Substring(0, colonPos);
					string portStr = line.Substring(colonPos + 1);
					try
					{
						ub = new UriBuilder(Uri.UriSchemeHttp, hostStr, Convert.ToInt32(portStr), "XiServices/serverDiscovery");
					}
					catch
					{
						ub = null;
						returnLines = CreateErrorLines("# An error was detected in the following \"machine name:port\" or \"IPv4 address:port\"",
														line);
					} // if not a valid host:port string
				}
				else // if not a valid address:port line
				{
					ub = null;
					returnLines = CreateErrorLines("# The following \"machine name:port\", \"IPv4 address:port\", or URL was not valid",
													line);
				}
			}
			else // a URL
			{
				// valid url: "http://USAUST-DEV127:58080/XiServices/serverDiscovery"
				try
				{
					ub = new UriBuilder(line);
					if (!((ub.Scheme == Uri.UriSchemeHttp) || (ub.Scheme == Uri.UriSchemeNetTcp))
						|| (string.IsNullOrEmpty(ub.Host))
						|| (ub.Port < 1)
						|| (string.IsNullOrEmpty(ub.Path))
					   )
					{
						ub = null;
						returnLines = CreateErrorLines("# The following URL was invalid", line);
					}
				}
				catch
				{
					ub = null;
					returnLines = CreateErrorLines("# The following URL was invalid", line);
				}
			}
			if (ub != null)
			{
				// replace "localhost" in the URL with the MachineName
				if (string.Compare("localhost", ub.Host) == 0)
					ub.Host = System.Environment.MachineName;
				outUrl = ub.Uri.OriginalString.ToUpper();
				returnLines.Add(line);
			}
			return returnLines;
		}

		/// <summary>
		/// This method creates error lines for the manual configuration section of the manual 
		/// configuration file.
		/// </summary>
		/// <param name="msg">The message line to use</param>
		/// <param name="line">The line in error</param>
		/// <returns></returns>
		private List<string> CreateErrorLines(string msg, string line)
		{
			List<string> returnLines = null;
			if (string.IsNullOrEmpty(line) == false)
			{
				returnLines = new List<string>();
				string returnLine = "#";
				returnLines.Add(returnLine);
				returnLine = msg;
				returnLines.Add(returnLine);
				returnLine = "# " + line;
				returnLines.Add(returnLine);
				returnLine = "#";
				returnLines.Add(returnLine);
			}
			return returnLines;
		}
		#endregion // Non-Public Members

	}
}
