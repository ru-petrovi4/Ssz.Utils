using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public static partial class DsBlocksFactory
    {
        #region construction and destruction

        static DsBlocksFactory()
        {
            Factories = new Func<string, DsModule, ComponentDsBlock?, DsBlockBase>?[7];
            DsBlockTypesDictionary = new Dictionary<string, ushort>(Factories.Length, StringComparer.InvariantCultureIgnoreCase);                    

            Factories[0] = (string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) => DsBlockBase.EmptyDsBlock;

            Factories[Device_DsBlockType] = (string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) =>
                new DeviceDsBlock(Device_DsBlockTypeString, Device_DsBlockType, tag, parentModule, parentComponentDsBlock);
            DsBlockTypesDictionary.Add(Device_DsBlockTypeString, Device_DsBlockType);

            Factories[Component_DsBlockType] = (string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) =>
                new ComponentDsBlock(Component_DsBlockTypeString, Component_DsBlockType, tag, parentModule, parentComponentDsBlock);
            DsBlockTypesDictionary.Add(Component_DsBlockTypeString, Component_DsBlockType);
                 
            Factories[Technology_Measurement_DsBlockType] = (string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) =>
                new Technology.MeasurementDsBlock(Technology_Measurement_DsBlockTypeString, Technology_Measurement_DsBlockType, tag, parentModule, parentComponentDsBlock);
            DsBlockTypesDictionary.Add(Technology_Measurement_DsBlockTypeString, Technology_Measurement_DsBlockType);
            
            Factories[Pcs_AnalogInput_DsBlockType] = (string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) =>
                new Pcs.AnalogInputDsBlock(Pcs_AnalogInput_DsBlockTypeString, Pcs_AnalogInput_DsBlockType, tag, parentModule, parentComponentDsBlock);
            DsBlockTypesDictionary.Add(Pcs_AnalogInput_DsBlockTypeString, Pcs_AnalogInput_DsBlockType);
            
            Factories[Journal_DsBlockType] = (string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) =>
                new JournalDsBlock(Journal_DsBlockTypeString, Journal_DsBlockType, tag, parentModule, parentComponentDsBlock);
            DsBlockTypesDictionary.Add(Journal_DsBlockTypeString, Journal_DsBlockType);
                 
            Factories[Sis_And_BockType] = (string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) =>
                new Sis.AndDsBlock(Sis_And_BockTypeString, Sis_And_BockType, tag, parentModule, parentComponentDsBlock);
            DsBlockTypesDictionary.Add(Sis_And_BockTypeString, Sis_And_BockType);            
        }

        #endregion

        #region public functions

        public const string Device_DsBlockTypeString = @"Device";

        public const string Component_DsBlockTypeString = @"Component";

        public const string Technology_Measurement_DsBlockTypeString = @"Technology.Measurement";

        public const string Pcs_AnalogInput_DsBlockTypeString = @"Pcs.AnalogInput";

        public const string Journal_DsBlockTypeString = @"Journal";

        public const string Sis_And_BockTypeString = @"Sis.And";

        public const UInt16 Device_DsBlockType = 1;

        public const UInt16 Component_DsBlockType = 2;

        public const UInt16 Technology_Measurement_DsBlockType = 3;

        public const UInt16 Pcs_AnalogInput_DsBlockType = 4;

        public const UInt16 Journal_DsBlockType = 5;

        public const UInt16 Sis_And_BockType = 6;

        /// <summary>
        ///     blockTypeString is Case-Insensitive
        /// </summary>
        /// <param name="blockTypeString"></param>
        /// <returns></returns>
        public static UInt16 GetDsBlockType(string blockTypeString)
        {
            if (String.IsNullOrEmpty(blockTypeString)) return 0;
            if (!DsBlockTypesDictionary.TryGetValue(blockTypeString, out UInt16 blockType))
                return 0;
            return blockType;
        }

        public static DsBlockBase CreateDsBlock(UInt16 blockType, string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {
            if (blockType >= Factories.Length) return DsBlockBase.EmptyDsBlock;
            var factory = Factories[blockType];
            if (factory is null) return DsBlockBase.EmptyDsBlock;
            return factory.Invoke(tag, parentModule, parentComponentDsBlock);
        }

        #endregion

        #region private fields

        private static readonly Func<string, DsModule, ComponentDsBlock?, DsBlockBase>?[] Factories;

        private static readonly Dictionary<string, UInt16> DsBlockTypesDictionary;

        #endregion
    }
}
