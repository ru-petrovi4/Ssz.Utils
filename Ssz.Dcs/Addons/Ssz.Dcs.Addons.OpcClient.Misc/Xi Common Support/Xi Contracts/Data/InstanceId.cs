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

namespace Xi.Contracts.Data
{
	/// <summary>
	/// <para>The InstanceId is a string that identifies an instance that 
	/// can be accessed through an Xi server.  The InstanceId closely resembles 
	/// a URL. It contains a resource type prefix, a system name/id qualifier 
	/// for the local identifier, and the local identifier itself. </para>
	/// <para>The resource type is terminated by a ':' that is followed by an 
	/// optional SystemId.  The local identifier follows and is always preceded and 
	/// terminated by a '/', producing an InstanceId of the form:</para>
	/// <para>"ResourceType:System/LocalId/" (SystemId is present), or</para>
	/// <para>"ResourceType:/LocalId/" (SystemId is NOT present), or</para>
	/// <para>The InstanceId may also contain an Element Identifier that can be used 
	/// to identify a specific element or range of elements of the identified object 
	/// (e.g. an element or series of elements of an array).  When present the 
	/// ElementId always follows the LocalId forward slash ('/') terminator and may 
	/// not itself contain a forward slash. See the ElementId property description for 
	/// more detail.</para> 
	/// <para>Each of these elements of the InstanceId is defined as a 
	/// property of the InstanceId string. See the description of each 
	/// of these properties for more detail.</para>
	/// <para>The following are examples of valid InstanceIds:</para>
	/// <para>"DA:MySystem/MyObject/" - identifies a data object in MySystem.</para>
	/// <para>"DA:MySystem/MyObject/[4]" - identifies the fifth element (at zero-based index 4)
	/// of a constructed (e.g array) data object in MySystem.</para>
	/// <para>"DA:/MyObject/" - identifies a data object using only it local id.</para>
	/// <para>"MySystem/MyObject/" - identifies an object in MySystem.</para>
	/// <para>":MySystem/MyObject/" - identifies an object in MySystem.</para>
	/// <para>":MyPlant.MySystem/MyObject/" - identifies an object in MySystem that is in MyPlant.</para>
	/// <para>"/MyObject/" - identifies an object using only it local id.</para>
	/// <para>":/MyObject/" - identifies an object using only it local id.</para>
	/// <para>"//" - identifies the root within the server.</para>
	/// <para>"DA://" - identifies the root for data objects within the server.</para>
	/// <para>"DA:MySystem//" - identifies the root for data objects in MySystem.</para>
	/// <para>The following are examples of invalid InstanceIds:</para>
	/// <para>"D:MySystem/MyObject" - The ResourceType must be at least 2 characters and 
	/// MyObject should be terminated by '/'.</para>
	/// <para>"MyObject" - the LocalId must be preceded and terminated by a '/'</para>
	/// <para>":MyPlant/MySystem/MyObject/" - not invalid, but identifies an object 
	/// in MyPlant whose local id is "MySystem/MyObject.  It should be more 
	/// appropriately named ":MyPlant.MySystem/MyObject/"</para>
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class InstanceId
	{
		#region Constants

		/// <summary>
		/// The InstanceId.FullyQualifiedId string for the Root.
		/// </summary>
		public const string RootId = "//";

		#endregion

		#region Data Members 

		/// <summary>
		/// != null
		/// <para>The complete instance identifier string composed of 
		/// the ResourceType, System, and LocalId.</para>
		/// </summary>
		[DataMember] public string FullyQualifiedId;

		#endregion

		#region Constructors
		/// <summary>
		/// This is the default constructor.  It creates an object 
		/// with a null identifier.
		/// </summary>
		public InstanceId()
		{
			FullyQualifiedId = "";
		}

		/// <summary>
		/// This constructor creates an InstanceId for the local server 
		/// using a fully qualified identifier or a local id.
		/// </summary>
		/// <param name="theId">
		/// The local id or the fully qualified id.
		/// </param>
		public InstanceId(string theId)
		{
			FullyQualifiedId = theId ?? "";
		}

		/// <summary>
		/// This constructor creates an InstanceId from an optional resource 
		/// type, an optional system and an identifier local to the system 
		/// that contains the instance. 
		/// </summary>
		/// <param name="resourceType">
		/// The ResourceType property. Null or empty if not used. If present,
		/// the constructor always inserts a '/' after the ResourceType during 
		/// construction of the FullyQualifiedId.
		/// </param>
		/// <param name="system">
		/// The System property. Null or empty if not used.
		/// </param>
		/// <param name="localId">
		/// The local id property. The constructor always inserts '/' before the 
		/// localId during construction of the FullyQualifiedId.
		/// </param>
		public InstanceId(string resourceType, string system, string localId)
		{
			if (!string.IsNullOrEmpty(localId))
			{
				// there is a local name
				FullyQualifiedId = "/" + localId + "/";
				if (string.IsNullOrEmpty(system) == false) // if there is a system
					FullyQualifiedId = system + FullyQualifiedId;
				if (string.IsNullOrEmpty(resourceType) == false) // if there is a system
					FullyQualifiedId = resourceType + ":" + FullyQualifiedId;
			}
			else
            {
				FullyQualifiedId = "";
			}
		}

		/// <summary>
		/// This constructor creates an InstanceId from an optional resource 
		/// type, an optional system, an identifier local to the system 
		/// that contains the instance, and an element id that identifies a 
		/// specific element or range of elements. 
		/// </summary>
		/// <param name="resourceType">
		/// The ResourceType property. Null or empty if not used. If present,
		/// the constructor always inserts a '/' after the ResourceType during 
		/// construction of the FullyQualifiedId.
		/// </param>
		/// <param name="system">
		/// The System property. Null or empty if not used.
		/// </param>
		/// <param name="localId">
		/// The local id property. The constructor always inserts '/' before the 
		/// localId during construction of the FullyQualifiedId.
		/// </param>
		/// <param name="elementId">
		/// The local id property. The constructor always inserts '/' before the 
		/// localId during construction of the FullyQualifiedId.
		/// </param>
		public InstanceId(string resourceType, string system, string localId, string elementId)
		{
			// there is a local name
			if (!string.IsNullOrEmpty(localId))
			{
				FullyQualifiedId = "/" + localId;
				// there is an element id
				if (string.IsNullOrEmpty(elementId) == false)
					FullyQualifiedId += "/" + elementId;
				// if there is a system
				if (string.IsNullOrEmpty(system) == false) 
					FullyQualifiedId = system + FullyQualifiedId;
				// if there is a resource type
				if (string.IsNullOrEmpty(resourceType) == false) 
					FullyQualifiedId = resourceType + ":" + FullyQualifiedId;
			}
			else
            {
				FullyQualifiedId = "";
			}
		}
		/// <summary>
		/// This constructor creates an InstanceId from another InstanceId.  
		/// </summary>
		/// <param name="instanceId">The instance id that is to be copied to 
		/// the InstanceId being created.</param>
		public InstanceId(InstanceId instanceId)
		{
			FullyQualifiedId = instanceId.FullyQualifiedId;
		}

		#endregion

		#region Properties

		/// <summary>
		/// <para>This optional property returns the portion of the InstanceId 
		/// between the beginning of the InstanceId and the first ':' character. 
		/// The standard ResourceTypes are defined by constants in this class.  
		/// The ResourceType must be at least two characters.</para>
		/// <para>These constants may be extended by the server by appending characters 
		/// (e.g. "DA1" and "DA2") to differentiate instances of the same ResourceType.  
		/// This is only necessary when the server wraps more than one underlying server 
		/// of the same ResourceType (e.g more than one OPC DA server). </para>
		/// </summary>
		public string ResourceType
		{
			get
			{
				string system = null;
				int pos = FullyQualifiedId.IndexOf("/");
				if (pos >= 1) // if found and not the first character
				{
					string temp = FullyQualifiedId.Substring(0, pos);
					pos = temp.IndexOf(":");
					if (pos > 1) // if and at least two characters
						system = temp.Substring(0, pos);
				}
				return system;
			}
		}

		/// <summary>
		/// <para>This optional property returns the portion of the InstanceId 
		/// between the ResourceType and the LocalId. The ':' delimiter that 
		/// terminates the ResourceType and '/' delimiter that starts the LocalId 
		/// are not included in the System.  The System value is not permitted 
		/// to contain the '/' character. </para>
		/// <para>The System value is specific to the server, but it is recommended 
		/// that it identify the system that contains the instance.  If the system 
		/// name is qualified by one or more higher level names, such as the SiteName, 
		/// it is recommended that they be separated from each other by '.' characters. 
		/// E.g. "Site#1.System#3".</para>  
		/// <para>If all InstanceIds provided by server are contained within the same 
		/// system, the System property may be omitted from InstanceIds. In this case, the 
		/// system name is contained in the ServerDescription object accessible via 
		/// the Identify() method.</para>
		/// </summary>
		public string System
		{
			get
			{
				string system = null;
				int pos = FullyQualifiedId.IndexOf("/");
				if (pos >= 1) // if found and not the first character
				{
					string temp = FullyQualifiedId.Substring(0, pos);
					pos = temp.IndexOf(":");
					if (pos < temp.Length - 1) // if before the last character of temp
						system = temp.Substring(pos + 1);
				}
				return system;
			}
		}

		/// <summary>
		/// This property returns the local id portion of the InstanceId.
		/// The local id identifies an instance within the identified system and site.
		/// It always follows the first '/' in the FullyQualifiedId.
		/// </summary>
		public string LocalId
		{
			get
			{
			    if (_localIdCache != null)
			        return _localIdCache;

				int posFirstSlash = FullyQualifiedId.IndexOf("/");
				if (posFirstSlash < 0)
					return null;
				int posLastSlash = FullyQualifiedId.LastIndexOf("/");
				if ((posLastSlash < 0) || ((posLastSlash - posFirstSlash) <= 1))
					return null;
				// The LocalId is the string between the first and last '/'
				_localIdCache = FullyQualifiedId.Substring(posFirstSlash + 1, posLastSlash - posFirstSlash - 1);

			    return _localIdCache;
			}
		}

		/// <summary>
		/// <para>This property returns the element id portion of the InstanceId.
		/// The element id is a zero-based index that identifies an element 
		/// of an array or structure, or a list of elements. </para>
		/// <para>This property always follows the last '/' in the FullyQualifiedId.</para>
		/// <para>Each index is contained within square brackets (e.g. [6], or [4][5] 
		/// for a two-dimensional array. Servers are free to define other constructs 
		/// to identify elements. The only constraint is that the ElementId 
		/// cannot contain a forward slash ('/').</para>
		/// <para>If a range of indexes are to be specified, two indexes separated by 
		/// a hyphen ('-') are contained within a pair of square brackets (e.g. [3-6]).
		/// In this case, the order, ascending or descending is determined by which 
		/// index of the pair is higher. For example, [6-3] indicates a descending order 
		/// of indexes of 6, 5, 4, and 3.</para>
		/// </summary>
		public string ElementId
		{
			get
			{
				int pos = FullyQualifiedId.LastIndexOf("/");
				if (pos < 0)
					return null;
				return FullyQualifiedId.Substring(pos + 1);
			}
		}

		#endregion

		#region Methods
		/// <summary>
		/// This static method checks the validity of an instance id.
		/// </summary>
		/// <param name="instanceId">
		/// The instance id to validate.
		/// </param>
		/// <returns>
		/// True if valid. False if not.
		/// </returns>
		public static bool IsValid(InstanceId instanceId)
		{
			bool valid = false;
			if ((instanceId != null) && (string.IsNullOrEmpty(instanceId.FullyQualifiedId) == false))
			{
				int pos = instanceId.FullyQualifiedId.IndexOf("/");
				if ((pos >= 0) && (pos < instanceId.FullyQualifiedId.Length - 1)) // between the beginning and second to last character inclusive
				{
					string prefix = instanceId.FullyQualifiedId.Substring(0, pos);
					pos = prefix.IndexOf(":");
					if (   (pos == -1) // no ResourceType - that's ok
						|| (pos == 0)  // no ResourceType - that's ok
						|| (pos > 1))  // ResourceType length > 2 - that's ok
					{
						valid = true;
					}
				}
			}
			return valid;
		}

		/// <summary>
		/// This method checks the validity of this instance id.
		/// </summary>
		/// <returns>
		/// True if valid. False if not.
		/// </returns>
		public bool IsValid()
		{
			bool valid = false;
			int pos = FullyQualifiedId.IndexOf("/");
			if ((pos >= 0) && (pos < FullyQualifiedId.Length - 1)) // between the beginning and second to last character inclusive
			{
				string prefix = FullyQualifiedId.Substring(0, pos);
				pos = prefix.IndexOf(":");
				if ((pos == -1) // no ResourceType - that's ok
					|| (pos == 0)  // no ResourceType - that's ok
					|| (pos > 1))  // ResourctType length > 2 - that's ok
				{
					valid = true;
				}
			}
			return valid;
		}


		/// <summary>
		/// This override returns the LocalId as the string representation of the InstanceId.
		/// </summary>
		/// <returns>
		/// The string representation of the InstanceId.
		/// </returns>
		public override string ToString()
		{
			return FullyQualifiedId;
		}

		#endregion //Methods

	    private string _localIdCache;
	}
}