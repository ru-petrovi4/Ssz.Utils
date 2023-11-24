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

using Xi.Contracts.Data;

namespace Xi.Common.Support.Extensions
{
	public static class TypeIdExt
	{
		public static XiDataTypeHandle BasicDataValueType(this TypeId typeId)
		{
			if (null != typeId.Namespace) return XiDataTypeHandle.DataValueTypeIUnknown;
			if (null != typeId.SchemaType) return XiDataTypeHandle.DataValueTypeIUnknown;

			if (0 == string.Compare(typeof(Single).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeFloat32;
			if (0 == string.Compare(typeof(Int32).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeInt32;
			if (0 == string.Compare(typeof(String).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeString;

			if (0 == string.Compare(typeof(SByte).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeInt8;
			if (0 == string.Compare(typeof(Int16).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeInt16;
			if (0 == string.Compare(typeof(Int64).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeInt64;

			if (0 == string.Compare(typeof(Byte).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeUInt8;
			if (0 == string.Compare(typeof(UInt16).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeUInt16;
			if (0 == string.Compare(typeof(UInt32).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeUint32;
			if (0 == string.Compare(typeof(UInt64).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeUInt64;

			if (0 == string.Compare(typeof(Double).ToString(), typeId.LocalId, true))
				return XiDataTypeHandle.DataValueTypeFloat64;

			return XiDataTypeHandle.DataValueTypeIUnknown;
		}

		public static XiDataTypeHandle BasicDataType(this Type type)
		{
			//TODO: Customize any additional data type that are needed and those not fully converted

			if (typeof(string) == type) return XiDataTypeHandle.DataValueTypeString;
			if (typeof(double) == type) return XiDataTypeHandle.DataValueTypeFloat64;
			if (typeof(float) == type) return XiDataTypeHandle.DataValueTypeFloat32;
			if (typeof(int) == type) return XiDataTypeHandle.DataValueTypeInt32;
			if (typeof(uint) == type) return XiDataTypeHandle.DataValueTypeUint32;
			if (typeof(short) == type) return XiDataTypeHandle.DataValueTypeInt16;
			if (typeof(ushort) == type) return XiDataTypeHandle.DataValueTypeUInt16;
			if (typeof(sbyte) == type) return XiDataTypeHandle.DataValueTypeInt8;
			if (typeof(byte) == type) return XiDataTypeHandle.DataValueTypeUInt8;
			if (typeof(long) == type) return XiDataTypeHandle.DataValueTypeInt64;
			if (typeof(ulong) == type) return XiDataTypeHandle.DataValueTypeUInt64;
			if (typeof(bool) == type) return XiDataTypeHandle.DataValueTypeBoolean;

			return XiDataTypeHandle.DataValueTypeIUnknown;
		}
	}
}
