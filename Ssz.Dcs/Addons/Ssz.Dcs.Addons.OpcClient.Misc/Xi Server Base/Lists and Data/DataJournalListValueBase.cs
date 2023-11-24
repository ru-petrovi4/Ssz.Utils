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

using System.Collections.Generic;

using Xi.Contracts.Data;

namespace Xi.Server.Base
{
	public class DataJournalListValueBase
		: ValueRoot
	{
		public DataJournalListValueBase(uint clientAlias, uint serverAlias)
			: base(clientAlias, serverAlias)
		{
		}

		public void UpdateDictionary(JournalDataValues dataValues)
		{
			JournalDataValues journalDataValues = null;
			if (_journalDataValues.TryGetValue(dataValues.Calculation, out journalDataValues))
			{
				_journalDataValues.Remove(dataValues.Calculation);
			}
			_journalDataValues.Add(dataValues.Calculation, dataValues);
		}

		protected Dictionary<TypeId, JournalDataValues> _journalDataValues =
			new Dictionary<TypeId, JournalDataValues>();
	}
}
