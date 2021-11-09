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
	/// This class defines a list of FilterCriterion that are logically ORed 
	/// together.
	/// </summary>
	[DataContract(Namespace = "urn:xi/data")]
	public class ORedFilters
	{
		/// <summary>
		/// The list of FilterCriterion that are to be ORed together.  If at least 
		/// one of the FilterCriterion results in TRUE, then the results for the 
		/// list are TRUE.
		/// </summary>
		[DataMember] public List<FilterCriterion>? FilterCriteria;

		/// <summary>
		/// <para>This method compares this ORedFilters against the filtersToCompare to determine 
		/// if they are identical. Identical ORedFilters are are those with the same number of 
		/// identical FilterCriterion that are in the same order. </para>
		/// <para>identical FilterCriterion are are those with the same operand, operator, and 
		/// comparison value.</para>
		/// </summary>
		/// <param name="filtersToCompare">
		/// The FilterSet to compare against this FilterSet.
		/// </param>
		/// <returns>
		/// Returns TRUE if the FilterSets are identical. Otherwise returns FALSE.
		/// </returns>
		public bool CompareIdentical(ORedFilters filtersToCompare)
		{
			bool bEqual = false;
			if ((this.FilterCriteria is not null)
				&& (filtersToCompare is not null)
				&& (filtersToCompare.FilterCriteria is not null)
			   )
			{
				if (this.FilterCriteria.Count == filtersToCompare.FilterCriteria.Count)
				{
					bEqual = true; // return true unless one of the FilterCriteria doesn't match
					for (int i = 0; i < this.FilterCriteria.Count; i++)
					{
						// return false as soon as one filter criterion doesn't match
						if (this.FilterCriteria[i].CompareIdentical(filtersToCompare.FilterCriteria[i]) == false)
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