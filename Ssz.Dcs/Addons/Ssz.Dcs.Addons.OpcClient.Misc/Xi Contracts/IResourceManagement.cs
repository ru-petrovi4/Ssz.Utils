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
using System.Collections.Generic;
using System.ServiceModel;
using Xi.Contracts.Data;

namespace Xi.Contracts
{
	/// <summary>
	/// <para>This interface is composed of methods used to:</para>
	/// <para>- initiate a context with the server, </para>
	/// <para>- read the server's MIB, </para>
	/// <para>- discover objects, alarms, and events supported by the server, </para>
	/// <para>- create lists within the server of selected objects, alarms, or 
	/// events, and</para> 
	/// <para>- create read and write endpoints and add one or more lists to them. </para>
	/// </summary>
	[ServiceContract(Namespace = "urn:xi/contracts")]
	public interface IResourceManagement
	{
		#region Context Management

		/// <summary>
		/// <para>This method is used to establish a context between 
		/// the client and the server.  The server must authenticate 
		/// the client when this method is called.</para> 
		/// <para>Once created, the context is capable of opening Read, 
		/// Write, Poll, or Callback endpoints at the request of the 
		/// client. Only one of each type may be opened for a given 
		/// context. See the OpenEndpoint() method for more information 
		/// about endpoints. </para>  
		/// <para>If the WCF connection to the Resource Management 
		/// Endpoint is inadvertanatly disconnected, the client can 
		/// prevent the context from timing-out and automatically 
		/// closing by calling the ReInitiate() method.</para>
		/// </summary>
		/// <param name="applicationName">
		/// The name of the client application.  
		/// </param>
		/// <param name="workstationName">
		/// The name of the workstation on which the client application 
		/// is running.  
		/// </param>
		/// <param name="localeId">
		/// The localeId to be used for the context. If the requested context 
		/// is not supported by the server, the server will return its default 
		/// context. 
		/// </param>
		/// <param name="contextTimeout">
		/// The context timeout is expressed in milliseconds.  If a client request 
		/// is not received on the context within this period of time, the server 
		/// will close the context because of inactivity.  The requested timeout 
		/// value can be negotiated up or down by the server. The negotiated 
		/// value is returned to the client.  A request value of zero causes 
		/// the server to use its default timeout.  If the underlying WCF 
		/// connection is inadvertantly terminated, the client may reopen the 
		/// context within this time-out period using the ReInitiate() method.
		/// </param>
		/// <param name="contextOptions">
		/// This parameter enables various debug and tracing options used to 
		/// aide in diagnosing issues. see ContextOptions enum for the 
		/// valid values.
		/// </param>
		/// <param name="reInitiateKey">
		/// A server-specific string that is to be supplied by the client in the 
		/// ReInitiate() method call. This parameter is used to prevent interloping 
		/// clients from re-initiating a context using only the context id that was 
		/// obtained through observing message sent to unencrypted Xi endpoints.  
		/// The reinitiate key value returned to the client by this method is 
		/// server-specific.   
		/// </param>
		/// <returns>
		/// The server generated context id.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		string Initiate(string applicationName, string workstationName, 
			ref uint localeId, ref uint contextTimeout, ref uint contextOptions, 
			out string reInitiateKey);

		/// <summary>
		/// This method is used to close a context. When the context 
		/// is closed, all resources/endpoints associated with the  
		/// context are released.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier of the context to close. 
		/// </param>
		[OperationContract, FaultContract(typeof(XiFault))]
		void Conclude(string contextId);		

		#endregion

		#region Info Discovery Methods

		/// <summary>
		/// <para>This method is used to get the description of the 
		/// server.  This method can be called before a context has 
		/// been established with the server.</para>
		/// </summary>
		/// <param name="contextId">
		/// The optional context identifier. This call can be issued 
		/// without first having established a client context.  
		/// However, the ServerDetails element of the ServerDescription 
		/// is not returned unless this parameter is present.
		/// </param>
		/// <returns>
		/// The description of the server. 
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		ServerDescription Identify(string contextId);

		/// <summary>
		/// This method is used to get the state of the server, and 
		/// the state of any wrapped servers.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <returns>
		/// The status of the Xi server and the status of wrapped servers. 
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<ServerStatus> Status(string contextId);

		/// <summary>
		/// <para>This method returns text descriptions of error codes.</para>  
		/// </summary>
		/// <param name="contextId">
		/// The context identifier. 
		/// </param>
		/// <param name="resultCodes">
		/// The result codes for which text descriptions are being requested.
		/// </param>
		/// <returns>
		/// The list of result codes and if a result code indicates success, 
		/// the requested text descriptions. The size and order of this 
		/// list matches the size and order of the resultCodes parameter.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<RequestedString> LookupResultCodes(string contextId, List<uint> resultCodes);

		/// <summary>
		/// <para>This method is used to find objects in the server.  The 
		/// client uses the findCriteria parameter to identify a starting 
		/// branch and a set of filter criteria.  It also specifies the 
		/// maximum number of objects to return.  </para> 
		/// <para>The server examines the objects that are children of the 
		/// specified branch and selects those that match the filter criteria.
		/// Note that "children" are objects whose root paths can be created 
		/// by appending their names to the path used to identify the starting  
		/// branch.</para>  
		/// <para>The object attributes of the selected objects are 
		/// returned to the client. The number returned is limited by the 
		/// number specified in the numberToReturn parameter.  If the number 
		/// returned is less than than that number, then the client can 
		/// safely assume that the server has no more to return.</para>  
		/// <para>However, if the number returned is equal to that number, 
		/// then the client can retrieve the next set of results by issuing 
		/// another FindObjects() call with the findCriteria parameter set to 
		/// null. A null findCriteria indicates to the server to continue 
		/// returning results from those remaining in the list.  The client 
		/// eventually detects the end of the list by receiving a response 
		/// that returns less than specified by the numberToReturn parameter.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="findCriteria">
		/// The criteria used by the server to find objects.  If this 
		/// parameter is null, then this call is a continuation of the 
		/// previous find.
		/// </param>
		/// <param name="numberToReturn">
		/// The maximum number of objects to return in a single response.
		/// </param>
		/// <returns>
		/// <para>The list of object attributes for the objects that met 
		/// the filter criteria. </para>  
		/// <para>Returns null if the starting object is a leaf, or no objects 
		/// were found that meet the filter criteria, or if the call was made 
		/// with a null findCriteria and there are no more objects to return.</para>
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<ObjectAttributes> FindObjects(string contextId, FindCriteria findCriteria, uint numberToReturn);

		/// <summary>
		/// <para>This method is used to find type definitions in the server.  The 
		/// client uses the findCriteria parameter to identify a starting 
		/// branch and a set of filter criteria.  It also specifies the 
		/// maximum number of types to return.  </para> 
		/// <para>The server examines the types that are children of the 
		/// specified branch and selects those that match the filter criteria.
		/// Note that "children" are types whose root paths can be created 
		/// by appending their names to the path used to identify the starting  
		/// branch.</para>  
		/// <para>The type attributes attributes of the selected types are 
		/// returned to the client. The number returned is limited by the 
		/// number specified in the numberToReturn parameter.  If the number 
		/// returned is less than than that number, then the client can 
		/// safely assume that the server has no more to return.</para>  
		/// <para>However, if the number returned is equal to that number, 
		/// then the client can retrieve the next set of results by issuing 
		/// another FindTypes() call with the findCriteria parameter set to 
		/// null.</para> 
		/// <para>A null findCriteria indicates to the server to continue 
		/// returning results from those remaining in the list.  The client 
		/// eventually detects the end of the list by receiving a response 
		/// that returns less than specified by the numberToReturn parameter.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="findCriteria">
		/// The criteria used by the server to find types.  If this 
		/// parameter is null, then this call is a continuation of the 
		/// previous find.
		/// </param>
		/// <param name="numberToReturn">
		/// The maximum number of objects to return in a single response.
		/// </param>
		/// <returns>
		/// <para>The list of type attributes for the type that met 
		/// the filter criteria. </para>  
		/// <para>Returns null if the starting type is a leaf, or no types 
		/// were found that meet the filter criteria, or if the call was made 
		/// with a null findCriteria and there are no more types to return.</para>
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<TypeAttributes> FindTypes(string contextId, FindCriteria findCriteria, uint numberToReturn);

		/// <summary>
		/// <para>Each discoverable object in the server has at least 
		/// one path to the root (called the root path).  A root path 
		/// is represented by an ordered list of object names beginning 
		/// with "Root" and ending with the name of the object.</para>
		/// <para>This method identifies an object by one of its root 
		/// paths and requests the server to return any additional root 
		/// paths that exist.  If there are no other root paths that can 
		/// be used to reach the object, then null is returned.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="objectPath">
		/// The root path that identifies the object for which alternate 
		/// root paths are being requested. 
		/// </param>
		/// <returns>
		/// The list of additional root paths to the specified object.  
		/// Null if specified objectPath is the only root path to the 
		/// object. An exception is thrown if the specified objectPath is 
		/// invalid.  
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<ObjectPath> FindRootPaths(string contextId, ObjectPath objectPath);
 
		#endregion		

		#region List Management
		/// <summary>
		/// <para>This method is used to create a list of data 
		/// objects or alarms/events within the context.  </para>
		/// <para>Lists are created in the disabled state, and must be 
		/// enabled to cause them to update data values or events.
		/// Creating them in the disabled state was done to improve 
		/// performance.  It allows the server to wait until data 
		/// list objects have been added before it begins its
		/// updating operation.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="clientId">
		/// The Client LocalId for this list.  Used in callbacks to allow the 
		/// client to identify this list.
		/// </param>
		/// <param name="listType">
		/// Indicates the type of list to be created.
		/// Standard list types as defined by the Xi.Contacts.Constants.StandardListType 
		/// enumeration are: 
		/// 1) Data List, 
		/// 2) History Data List, 
		/// 3) Event List 
		/// 4) History Event List
		/// </param>
		/// <param name="updateRate">
		/// The requested update rate in milliseconds for the list. The  
		/// update rate indicates how often the server updates the 
		/// values of elements in the list.  A value of 0 indicates 
		/// that updating is exception-based. The server may negotiate 
		/// this value, up or down as necessary to support its efficient 
		/// operation.
		/// </param>
		/// <param name="bufferingRate">
		/// <para>An optional-use parameter that indicates that the server is 
		/// to buffer data updates, rather than overwriting them, until either 
		/// the time span defined by the buffering rate expires or the values 
		/// are transmitted to the client in a callback or poll response. If 
		/// the time span expires, then the oldest value for a data object is 
		/// discarded when a new value is received from the underlying system.</para>
		/// <para>The value of the bufferingRate is set to 0 to indicate 
		/// that it is not to be used and that new values overwrite (replace) existing 
		/// cached values.  </para>
		/// <para>When used, this parameter contains the client-requested buffering 
		/// rate, which the server may negotiate up or down, or to 0 if the 
		/// server does not support the buffering rate. </para>
		/// <para>The FeaturesSupported member of the StandardMib is used to indicate 
		/// server support for the buffering rate.</para>
		/// </param>
		/// <param name="filterSet">
		/// The set of filters to be used to select the elements of the list.  
		/// </param>
		/// <returns>
		/// The attributes created for the list.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		ListAttributes DefineList(string contextId, uint clientId, uint listType,
			uint updateRate, uint bufferingRate, FilterSet filterSet);

		/// <summary>
		/// <para>This method gets the attributes of a list.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listIds">
		/// The identifiers for the lists whose attributes are to be 
		/// retrieved. If this parameter is null, then the attributes for 
		/// all lists in the context are to be returned.
		/// </param>
		/// <returns>
		/// The list of requested List Attributes. The size and order 
		/// of this list matches the size and order of the listAliases 
		/// parameter.  
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<ListAttributes> GetListAttributes(string contextId, List<uint> listIds);

		/// <summary>
		/// This method is used to renew the aliases for a list.  Successful completion 
		/// of this method invalidates the previous server alias, but not the previous 
		/// client alias. However, the server begins using the new client alias at its 
		/// earliest opportunity and ceases using the previous client alias. This behavior 
		/// accommodates the race condition that may occur when this method is being processed 
		/// by the server concurrently with the publishing of data to the client.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the list whose aliases are to be 
		/// updated.
		/// </param>
		/// <param name="newAliases">
		/// The list of aliases to be updated. Each AliasUpdate in the list 
		/// contains the existing server alias and new client alias for it.
		/// </param>
		/// <returns>
		/// The list of updated aliases. The size and order of this list matches 
		/// the size and order of the listAliases parameter.  Each AliasResult 
		/// in the list contains the new client alias from the request and its 
		/// corresponding new server alias assigned by the server.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<AliasResult> RenewAliases(string contextId, uint listId, List<AliasUpdate> newAliases);

		/// <summary>
		/// <para>This method deletes one or more lists for the specified context.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listIds">
		/// The identifiers for the lists to be deleted.  If this parameter is null,
		/// then all lists in the context are to be deleted.
		/// </param>
		/// <returns>
		/// The list identifiers and result codes for the lists whose 
		/// deletion failed. Returns null if all deletes succeeded.  
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<AliasResult> DeleteLists(string contextId, List<uint> listIds);

		/// <summary>
		/// <para>This method is used to add objects to a list.  Objects 
		/// are added with updating of their values by the server 
		/// disabled. Updating of values by the server can be enabled 
		/// using the EnableListUpdating() method.</para>
		/// <para>For performance reasons, data objects should not be 
		/// added one at a time by clients. Clients should, instead,
		/// create a list of data objects and submit them all together 
		/// to be added to the data list.  </para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the list to which data objects are to be 
		/// added.
		/// </param>
		/// <param name="dataObjectsToAdd">
		/// The data objects to add.
		/// </param>
		/// <returns>
		/// The list of results. The size and order of this list matches 
		/// the size and order of the objectsToAdd parameter.   
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<AddDataObjectResult> AddDataObjectsToList(string contextId, uint listId,
							List<ListInstanceId> dataObjectsToAdd);

		/// <summary>
		/// <para>This method is used to remove members from a list.  
		/// It does not, however, delete the corresponding data object 
		/// from the server.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the list from which data objects are 
		/// to be removed.
		/// </param>
		/// <param name="serverAliasesToDelete">
		/// <para>The server aliases of the data objects to remove. When this value 
		/// is null all elements of the list are to be removed.</para> 
		/// <para>If the value of a serverAlias in this list is zero, then its 
		/// zero-based index into the serverAliases list is used as the ClientAlias 
		/// in a returned AliasResult. </para>
		/// </param>
		/// <returns>
		/// <para>Returns null if all removals succeeded. If not, returns the 
		/// client and server aliases and result codes for the data objects that could 
		/// not be removed. Data objects that were successfully removed are not included 
		/// in this list. See XiFaultCodes claass for standardized result codes.</para> 
		/// <para>If the value of a serverAlias in the serverAliases list is zero, 
		/// then its zero-based index into the serverAliases list is used as the 
		/// ClientAlias in a returned AliasResult to allow the client to locate the 
		/// entry in the submitted list of serverAliases.</para>
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<AliasResult> RemoveDataObjectsFromList(string contextId, uint listId,
							List<uint> serverAliasesToDelete);

		/// <summary>
		/// This method is used to change the update rate, buffering rate, and/or 
		/// filter set of a list.  The new value replace the old values if they exist.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the list for which the filters are to 
		/// be changed.
		/// </param>
		/// <param name="updateRate">
		/// The new update rate of the list.  The server will negotiate this rate to one 
		/// that it can support.  GetListAttributes can be used to obtain the current 
		/// value of this parameter.  Null if the update rate is not to be updated.  
		/// </param>
		/// <param name="bufferingRate">
		/// The new buffering rate of the list.  The server will negotiate this rate to one 
		/// that it can support.  GetListAttributes can be used to obtain the current 
		/// value of this parameter.  Null if the buffering rate is not to be updated.
		/// </param>
		/// <param name="filterSet">
		/// The new set of filters.  The server will negotiate these filters to those 
		/// that it can support.  GetListAttributes can be used to obtain the current 
		/// value of this parameter.  Null if the filters are not to be updated.
		/// </param>
		/// <returns>
		/// The revised update rate, buffering rate, and filter set.  Attributes 
		/// that were not updated are set to null in this response.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		ModifyListAttrsResult ModifyListAttributes(string contextId, uint listId,
								Nullable<uint> updateRate, Nullable<uint> bufferingRate, FilterSet filterSet);

		/// <summary>
		/// <para>This method is used to enable or disable updating of an entire 
		/// list. When this method is called, the enabled state of the list is changed, 
		/// but the enabled state of the individual elements of the list is unchanged. </para>
		/// <para>When a list is disabled, the server excludes it from participating in 
		/// callbacks and polls. However, at the option of the server, the server may continue 
		/// updating its cache for the elements of the list.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the list for which updating is to be 
		/// enabled or disabled.
		///</param>
		/// <param name="enable">
		/// Indicates, when TRUE, that updating of the list is to be enabled,
		/// and when FALSE, that updating of the list is to be disabled.
		/// </param>
		/// <returns>
		/// The attributes of the list.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		ListAttributes EnableListUpdating(string contextId, uint listId, bool enable);

		/// <summary>
		/// <para>This method is used to enable or disable updating of 
		/// individual entries of a list.  If the server aliases parameter is 
		/// null, then all entries of the list are enabled/disabled.  This call 
		/// does not change the enabled state of the list itself.</para>
		/// <para>When an element of the list is disabled, the server excludes it 
		/// from participating in callbacks and polls. However, at the option of the 
		/// server, the server may continue updating its cache for the element.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the list for which updating is to be 
		/// enabled or disabled.
		///</param>
		/// <param name="enableUpdating">
		/// Indicates, when TRUE, that updating of the list is to be enabled,
		/// and when FALSE, that updating of the list is to be disabled.
		/// </param>
		/// <param name="serverAliases">
		/// <para>The list of aliases for data objects of a list for which updating 
		/// is to be enabled or disabled.  When this value is null updating of all 
		/// elements of the list is to be enabled/disabled. In this case, however, 
		/// the enable/disable state of the list itself is not changed.</para>
		/// <para>If the value of a serverAlias in this list is zero, then its 
		/// zero-based index into the serverAliases list is used as the ClientAlias 
		/// in a returned AliasResult. </para>
		/// </param>
		/// <returns>
		/// <para>Returns null if the server was able to successfully enable/disable 
		/// the the specified elements for the specified list.  If not, returns the 
		/// client and server aliases and result codes for the data objects that could 
		/// not be enabled/disabled. Data objects that were successfully enabled/disabled 
		/// are not included in this list. See XiFaultCodes claass for standardized 
		/// result codes. </para> 
		/// <para>If the value of a serverAlias in the serverAliases list is zero, 
		/// then its zero-based index into the serverAliases list is used as the 
		/// ClientAlias in a returned AliasResult to allow the client to locate the 
		/// entry in the submitted list of serverAliases.</para>
		/// <para>Throws an exception if the specified context or list could not be found.</para> 
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<AliasResult> EnableListElementUpdating(string contextId, uint listId,
							bool enableUpdating, List<uint> serverAliases);

		/// <summary>
		/// This method is used to request that category-specific fields be 
		/// included in event messages generated for alarms and events of 
		/// the category for the specified Event/Alarm List.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the list for which event message fields are being added. 
		///</param>
		/// <param name="categoryId">
		/// The category for which event message fields are being added.
		/// </param>
		/// <param name="fieldObjectTypeIds">
		/// The list of category-specific fields to be included in the event 
		/// messages generated for alarms and events of the category.  Each field 
		/// is identified by its ObjectType LocalId obtained from the EventMessageFields 
		/// contained in the EventCategoryConfigurations Standard MIB element.
		/// </param>
		/// <returns>
		/// The ObjectTypeIds and result codes for the fields that could not be 
		/// added to the event message. Returns null if all succeeded.  
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<TypeIdResult> AddEventMessageFields(string contextId, uint listId, uint categoryId, List<TypeId> fieldObjectTypeIds);

		/// <summary>
		/// <para>This method is used to cause one or more data objects of 
		/// a list to be "touched".  Data objects that are in the disabled 
		/// state (see the EnableListElementUpdating() method) are not 
		/// affected by this method.  This method cannot be used with 
		/// event lists.</para>
		/// <para>Touching an enabled data object causes the server to update 
		/// the data object, mark it as changed (even if their values did not change), 
		/// and then return it to the client in the next callback or poll.</para> 
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The server identifier for the lists whose data objects are to be touched.
		/// </param>
		/// <param name="serverAliases">
		/// <para>The aliases for the data objects to touch. When this value is null all 
		/// elements of the list are to be touched.</para>
		/// <para>If the value of a serverAlias in this list is zero, then its 
		/// zero-based index into the serverAliases list is used as the ClientAlias 
		/// in a returned AliasResult. </para>
		/// </param>
		/// <returns>
		/// <para>Returns null if the server was able to successfully enable/disable 
		/// the the specified elements for the specified list.  If not, returns the 
		/// client and server aliases and result codes for the data objects that could 
		/// not be touched. Data objects that were successfully touched are not included 
		/// in this list. See XiFaultCodes claass for standardized result codes. </para>
		/// <para>If the value of a serverAlias in the serverAliases list is zero, 
		/// then its zero-based index into the serverAliases list is used as the 
		/// ClientAlias in a returned AliasResult to allow the client to locate the 
		/// entry in the submitted list of serverAliases.</para>
		/// <para>Throws an exception if the specified context or list could not be found.</para> 
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<AliasResult> TouchDataObjects(string contextId, uint listId, List<uint> serverAliases);

		/// <summary>
		/// <para>This method is used to cause a list to be "touched".</para> 
		/// <para>For lists that contain data objects, this method causes 
		/// the server to update all data objects in the list that are currently 
		/// enabled (see the EnableListElementUpdating() method), mark them 
		/// as changed (even if their values did not change), and then return 
		/// them all to the client in the next callback or poll. </para>  
		/// <para>For lists that contain events, this method causes 
		/// the server to mark all alarms/event in the list as changed, 
		/// and then return them all to the client in the next callback or poll.</para>
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="listId">
		/// The identifier for the list to be touched.
		///</param>
		/// <returns>
		/// The result code for the operation.  See XiFaultCodes class for 
		/// standardized result codes. 
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		uint TouchList(string contextId, uint listId);

		#endregion

		#region Alarms and Events

		/// <summary>
		/// This method is used to request summary information for the 
		/// alarms that can be generated for a given event source.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="eventSourceId">
		/// The InstanceId for the event source for which alarm summaries are 
		/// being requested.
		/// </param>
		/// <returns>
		/// The summaries of the alarms that can be generated by the specified 
		/// event source.  
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<AlarmSummary> GetAlarmSummary(string contextId, InstanceId eventSourceId);

		/// <summary>
		/// This method is used to enable or disable alarms for a specified area or event source. 
		/// It is independent of the XiLists on which the alarms may be reported.
		/// It throws a fault if the requested operation cannot be performed successfully.
		/// </summary>
		/// <param name="contextId">
		/// The context identifier.
		/// </param>
		/// <param name="enableFlag">
		/// This flag indicates, when TRUE, that alarms are to be enabled, and when FALSE, that they 
		/// are to be disabled.
		/// </param>
		/// <param name="areaFlag">
		/// This flag indicates, when TRUE, that the eventContainerIds parameter contains a list of 
		/// InstanceIds for areas, and when FALSE, that it contains a list of InstanceIds for event sources.
		/// </param>
		/// <param name="eventContainerIds">
		/// The InstanceId for the area or the event source for which alarms are to be enabled or disabled.
		/// </param>
		/// <returns>Null if all requested enable/disable operations succeeded. Otherwise, the list of result codes. The size and 
		/// order of this list matches that of the eventContainerIds.  Standard result code values are defined by 
		/// the Xi.Contracts.Constants.XiFaultCodes class. There is one result code for each eventContainerId.</returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<UInt32> EnableAlarms(string contextId, bool enableFlag, bool areaFlag, List<InstanceId> eventContainerIds);

		/// <summary>
		/// This method returns the enable state for a specified area or event source.
		/// It throws a fault if the requested operation cannot be performed successfully.
		/// </summary>
		/// <param name="contextId">The context identifier.</param>
		/// <param name="areaFlag">
		/// This flag indicates, when TRUE, that the eventContainerIds parameter contains a list of 
		/// InstanceIds for areas, and when FALSE, that it contains a list of InstanceIds for event sources.</param>
		/// <param name="eventContainerIds">
		/// The InstanceId for the area or the event source for which alarms are to be enabled or disabled.
		/// </param>
		/// <returns>An object with the enabled state and result code for each requested InstanceId.
		/// </returns>
		[OperationContract, FaultContract(typeof(XiFault))]
		List<AlarmEnabledState> GetAlarmsEnabledState(string contextId, bool areaFlag, List<InstanceId> eventContainerIds);

		#endregion

	}
}
