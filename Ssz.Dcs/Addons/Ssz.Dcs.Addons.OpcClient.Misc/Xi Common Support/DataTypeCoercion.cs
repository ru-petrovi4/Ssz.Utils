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
using System.Linq;
using System.Text;

namespace Xi.Common.Support
{
	public static class DataTypeCoercion
	{
		public static object CoerceDataType(XiDataTypeHandle fromType, XiDataTypeHandle toType, object value)
		{
			//TODO: Customize any additional data type converions that are needed and those not fully converted
			object coercedValue = null;
			try
			{
				switch (toType)
				{
					case XiDataTypeHandle.DataValueTypeString:
						coercedValue = value.ToString();
						break;

					case XiDataTypeHandle.DataValueTypeFloat64:
						coercedValue = Convert.ToDouble(value);
						break;

					case XiDataTypeHandle.DataValueTypeFloat32:
						coercedValue = Convert.ToSingle(value);
						break;

					case XiDataTypeHandle.DataValueTypeInt32:
						coercedValue = Convert.ToInt32(value);
						break;

					case XiDataTypeHandle.DataValueTypeUint32:
						coercedValue = Convert.ToUInt32(value);
						break;

					case XiDataTypeHandle.DataValueTypeInt16:
						coercedValue = Convert.ToInt16(value);
						break;

					case XiDataTypeHandle.DataValueTypeUInt16:
						coercedValue = Convert.ToUInt16(value);
						break;

					case XiDataTypeHandle.DataValueTypeInt8:
						coercedValue = Convert.ToSByte(value);
						break;

					case XiDataTypeHandle.DataValueTypeUInt8:
						coercedValue = Convert.ToByte(value);
						break;

					case XiDataTypeHandle.DataValueTypeInt64:
						coercedValue = Convert.ToInt64(value);
						break;

					case XiDataTypeHandle.DataValueTypeUInt64:
						coercedValue = Convert.ToUInt64(value);
						break;

					case XiDataTypeHandle.DataValueTypeBoolean:
						coercedValue = Convert.ToBoolean(value);
						break;

					default:
						break;
				}
			}
			catch { }

			return coercedValue;
		}

	}
}
