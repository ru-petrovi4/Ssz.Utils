using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public static class DsConnectionsFactory
    {
        #region construction and destruction

        static DsConnectionsFactory()
        {
            Factories = new Func<DsModule, ComponentDsBlock?, DsConnectionBase?>?[3];
            ConnectionTypesDictionary = new Dictionary<string, byte>(Factories.Length, StringComparer.InvariantCultureIgnoreCase);                     

            Factories[0] = (DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) => null;
                    
            Factories[Ref_ConnectionType] = (DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) =>
                new RefDsConnection(Ref_ConnectionTypeString, Ref_ConnectionType, parentModule, parentComponentDsBlock);
            ConnectionTypesDictionary.Add(Ref_ConnectionTypeString, Ref_ConnectionType);
            
            Factories[Da_ConnectionType] = (DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) =>
                new DaDsConnection(Da_ConnectionTypeString, Da_ConnectionType, parentModule, parentComponentDsBlock);
            ConnectionTypesDictionary.Add(Da_ConnectionTypeString, Da_ConnectionType);            
        }

        #endregion

        #region public functions

        public const string Ref_ConnectionTypeString = @"REF";

        public const string Da_ConnectionTypeString = @"DA";

        public const byte Ref_ConnectionType = 1;

        public const byte Da_ConnectionType = 2;

        /// <summary>
        ///     Removes prefix from string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte GetConnectionTypeFromPrefix(ref string value)
        {
            if (value.StartsWith(Ref_ConnectionTypeString + "::"))
            {
                value = value.Substring(0, Ref_ConnectionTypeString.Length + 2);
                return Ref_ConnectionType;
            }
            if (value.StartsWith(Da_ConnectionTypeString + "::"))
            {
                value = value.Substring(0, Da_ConnectionTypeString.Length + 2);
                return Da_ConnectionType;
            }
            return 0;
        }

        /// <summary>
        ///     connectionTypeString is Case-Insensitive
        /// </summary>
        /// <param name="connectionTypeString"></param>
        /// <returns></returns>
        public static byte GetConnectionType(string connectionTypeString)
        {
            if (String.IsNullOrEmpty(connectionTypeString)) return 0;
            if (!ConnectionTypesDictionary.TryGetValue(connectionTypeString, out byte connectionType))
                return 0;
            return connectionType;
        }

        public static DsConnectionBase? CreateConnection(byte connectionType, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {            
            var factory = Factories[connectionType];
            if (factory is null) return null;
            return factory.Invoke(parentModule, parentComponentDsBlock);
        }

        public static RefDsConnection CreateRefConnection(string blockFullNameWithParamFullName, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {
            return new RefDsConnection(Ref_ConnectionTypeString, Ref_ConnectionType, parentModule, parentComponentDsBlock)
            {
                ConnectionString = blockFullNameWithParamFullName
            };
        }

        public static RefDsConnection CreateRefConnection(DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {
            return new RefDsConnection(Ref_ConnectionTypeString, Ref_ConnectionType, parentModule, parentComponentDsBlock);
        }

        #endregion

        #region private fields

        private static readonly Func<DsModule, ComponentDsBlock?, DsConnectionBase?>?[] Factories;

        private static readonly Dictionary<string, byte> ConnectionTypesDictionary;

        #endregion
    }
}
