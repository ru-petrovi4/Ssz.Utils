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

namespace Xi.Contracts.Data
{
	/// <summary>
	/// <para>This class is used to define the set of filters used by the server to 
	/// find objects.  The filter set is composed of a list of ORedFilters that 
	/// are logically ANDed together.  That is, to result in a value of TRUE, 
	/// each ORedFilters element in the list must result in TRUE.</para>
	/// <para>The boolean Not member is provided to allow DeMorgan's Theorem to be 
	/// used to convert an ORed expression of type:</para>
	/// <para>"((A AND B) OR (C AND D))" to 
	/// <para>"!((!A OR !B) AND (!C OR !D))",</para> 
	/// <para>where '!' represents "NOT".</para>
	/// </para>
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class FilterSet
	{
		/// <summary>
		/// Set to TRUE to negate the resuls of the Filters parameter.
		/// </summary>
		[DataMember] public bool Not;

		/// <summary>
		/// <para>The list of ORedFilters. Each element of the list contains its own list 
		/// of filters that are logically ORed together, and each element must resolve to 
		/// TRUE for this list of ORed filters to result in TRUE. That is, the elements of 
		/// this list are ANDed together, while the elements of each of the ORedFilters 
		/// are ORed together.</para>
		/// <para>For example, the expression "((A OR B) AND (C OR D))" would be represented 
		/// by setting "Not" to false and a "Filters" list containing two ORedFilters, one 
		/// that contains "(A OR B)", and one that contains "(C OR D)". </para>
		/// </summary>
		[DataMember] public List<ORedFilters>? Filters;

		/// <summary>
		/// <para>This method compares this FilterSet against the setToCompare to determine 
		/// if they are identical. Identical FilterSets are those with the same number of 
		/// ORedFilters that are identical and in the same order.  </para>
		/// <para>Identical ORedFilters are are those with the same number of FilterCriterion 
		/// that are identical and in the same order. </para>
		/// </summary>
		/// <param name="setToCompare">
		/// The FilterSet to compare against this FilterSet.
		/// </param>
		/// <returns>
		/// Returns TRUE if the FilterSets are identical. Otherwise returns FALSE.
		/// </returns>
		public bool CompareIdentical(FilterSet setToCompare)
		{
			bool bEqual = false;
			if ((this.Filters is not null)
				&& (setToCompare is not null)
				&& (setToCompare.Filters is not null)
			   )
			{
				// see if the number of ORedFilters are the same
				if (this.Filters.Count == setToCompare.Filters.Count)
				{
					// if so, for each ORed filter, see if the number of FilterCriterion are the same
					// then check to see if the FilterCriterion are the same
					for (int i = 0; i < this.Filters.Count; i++)
					{
						if (this.Filters[i].CompareIdentical(setToCompare.Filters[i]) == false)
						{
							return false;
						}
					}
				}
			}
			return bEqual;
		}
	}
}