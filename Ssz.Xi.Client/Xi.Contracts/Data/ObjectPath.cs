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

using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System;

namespace Xi.Contracts.Data
{
	/// <summary>
	/// The path to a specific object or type. 
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ObjectPath : IExtensibleDataObject
	{
		/// <summary>
		/// This member supports the addition of new members to a data 
		/// contract class by recording versioning information about it.  
		/// </summary>
		ExtensionDataObject? IExtensibleDataObject.ExtensionData { get; set; }
		
		#region Data Members
		/// <summary>
		/// <para>The path to an object (or type), either from the root or from another 
		/// object (or type).  The first element in the list is always an InstanceId string 
		/// or a TypeId string.  The InstanceId or TypeId for the root is "//".</para>
		/// <para>The remaining elements are object/type names (the Name member of the 
		/// ObjectAttributes or TypeAttributes class). Clients may construct this portion 
		/// of the path using the names of objects/types returned by the server in FindObjects() 
		/// or FindTypes() calls.  </para>
		/// <para>The last element in the list is always the name of an object/type, and 
		/// the intervening elements represent the names of objects/types (or branches) between 
		/// the root and the object/type identified by last element.</para>
		/// </summary>
		[DataMember] public List<string>? Elements;
		#endregion

		#region Constructors
		/// <summary>
		/// This constructor creates an object path with a root element.
		/// </summary>
		/// <param name="rootPath">
		/// Indicates, when TRUE, that the path is to be a root path that 
		/// starts with the root element.
		/// </param>
		public ObjectPath(bool rootPath)
		{
			Elements = new List<string>();
			if (rootPath)
				Elements.Add("//");
		}

		/// <summary>
		/// This constructor creates a copy of an existing path.
		/// </summary>
		/// <param name="path">
		/// The existing path to copy to the new path.
		/// </param>
		public ObjectPath(ObjectPath path)
		{
			if (Elements == null) throw new InvalidOperationException();
			if (path.Elements != null)
			{
				foreach (var str in path.Elements)
					Elements.Add(str);
			}
		}

		/// <summary>
		/// This constructor creates an object path from a starting InstanceId 
		/// or TypeId string and a '/' delimited string of object/type names.
		/// </summary>
		/// <param name="startingIdString">
		/// The starting InstanceId.FullyQualifiedId or the TypeId string obtained 
		/// using the TypeId.ToString() method.
		/// </param>
		/// <param name="stringPath">
		/// The '/' delimited string of object/type names.
		/// </param>
		public ObjectPath(string startingIdString, string stringPath)
		{
			if (!string.IsNullOrEmpty(startingIdString))
			{
				Elements = new List<string>();
				Elements.Add(startingIdString);
				if (!string.IsNullOrEmpty(stringPath))
				{
					string workingString = stringPath;
					int pos = workingString.IndexOf('/');
					while (workingString.Length > 0)
					{
						if (pos < 0) // no slash was found
						{
							if (Elements == null) throw new InvalidOperationException();
							Elements.Add(workingString);
							workingString = "";
						}
						else if (pos == 0) // Error - two slashes in a row or three initial slashes
						{
							Elements = null; // set Elements to null, since there was an error
							workingString = "";
						}
						else
						{
							if (Elements == null) throw new InvalidOperationException();
							Elements.Add(workingString.Substring(0, pos));
							workingString = workingString.Substring(pos + 1);
							pos = workingString.IndexOf('/');
						}
					}
				}
			}
		}
		#endregion

		#region Methods
		/// <summary>
		/// This method converts an object path to a string, using '/' characters to delimit 
		/// elements of the path.
		/// </summary>
		/// <returns>
		/// A string of '/' delimited path elements.
		/// </returns>
		public override string ToString()
		{
			if ((Elements == null) || (Elements.Count == 0)) return string.Empty;

			StringBuilder stringPath = new StringBuilder(byte.MaxValue);  //255
			stringPath.Append(Elements[0]);
			for (int i = 1; i < Elements.Count; i++)
			{
				stringPath.Append("/");
				stringPath.Append(Elements[i]);
			}
			return stringPath.ToString();
		}

		/// <summary>
		/// This method sets the list of elements to null.
		/// </summary>
		public void Clear()
		{
			Elements = new List<string>();
		}

		/// <summary>
		/// This method compares two paths and determines if the first path is 
		/// equal to or a parent of the second path. 
		/// </summary>
		/// <param name="firstPath">
		/// The first path in the comparison.
		/// </param>
		/// <param name="secondPath">
		/// The second path in the comparison.
		/// </param>
		/// <returns>
		/// The results of the comparison. 0 if the first path is equal to 
		/// the second path and 1 if the second path is the parent of the 
		/// first path. -1 if neither.
		/// </returns>
		public static int IsEqualToOrChildOf(ObjectPath firstPath, ObjectPath secondPath)
		{
			int results = -1;
			// if the first path is the root
			if (   (firstPath == null) 
				|| (firstPath.Elements == null) 
				|| (firstPath.Elements.Count == 0) 
				|| ( ((firstPath.Elements.Count == 1) && firstPath.Elements[0] == InstanceId.RootId))
			   )
			{
				// ...and the second path is the root
				if (   (secondPath == null) 
					|| (secondPath.Elements == null) 
					|| (secondPath.Elements.Count == 0) 
					|| ( ((secondPath.Elements.Count == 1) && secondPath.Elements[0] == InstanceId.RootId))
				   )
				{
					results = 0; // the paths are equal
				}
				else
				{
					results = -1; // the second path is not equal to the first path or its parent
				}
			}
			else // the first path is not the root 
			{
				// if the second path is the implicit root, see if the first path starts with the root
				if (   (secondPath == null)
					|| (secondPath.Elements == null)
					|| (secondPath.Elements.Count == 0)
				   )
				{
					if (firstPath.Elements[0] == InstanceId.RootId)
					{
						results = 1; // the second path is the parent of the first
					}
					else
					{
						results = -1; // the second path is not the parent of the first
					}
				}

				// Otherwise, the second path is not empty, 
				// so see if the first elements of each path match. 
				// If not the second path is not the parent of the first path
				else if (firstPath.Elements[0] != secondPath.Elements[0]) 
				{
					results = -1; // the second path is not the parent of the first
				}
				else //the first elements match, so check the subsequent elements
				{
					// if the same number of elements, or if the first path is longer 
					// than the second, the second path may be a parent of the first
					if (firstPath.Elements.Count >= secondPath.Elements.Count)
					{
						bool equal = true;
						for (int i = 0; i < secondPath.Elements.Count; i++)
						{
							if (firstPath.Elements[i] != secondPath.Elements[i])
							{
								equal = false;
								break;
							}
						}
						if (equal)
						{
							if (secondPath.Elements.Count == firstPath.Elements.Count)
								results = 0;
							else
								results = 1;
						}
					}
				}
			}
			return results;
		}
		#endregion
	}

}