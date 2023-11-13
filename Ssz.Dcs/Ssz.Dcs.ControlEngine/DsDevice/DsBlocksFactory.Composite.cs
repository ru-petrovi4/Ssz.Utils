using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public static partial class DsBlocksFactory
    {
        #region public functions        

        public static ComponentDsBlock CreateComponentDsBlock(string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {
            return (ComponentDsBlock)CreateDsBlock(Component_DsBlockType, tag, parentModule, parentComponentDsBlock);
        }

        public static ComponentDsBlock? CreateStandardTemplateComponentDsBlock(string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock, string template)
        {
            var componentDsBlock = (ComponentDsBlock)CreateDsBlock(Component_DsBlockType, tag, parentModule, parentComponentDsBlock);
            componentDsBlock.IsTemplate = true;

            switch (template.ToUpperInvariant())
            {
                case @"AI":
                    CreateAITemplateComponentDsBlock(componentDsBlock);                    
                    return componentDsBlock;
                case @"PID":
                    CreatePIDTemplateComponentDsBlock(componentDsBlock);                    
                    return componentDsBlock;
                default:
                    return null;
            }            
        }

        #endregion

        #region private functions

        private static void CreateAITemplateComponentDsBlock(ComponentDsBlock componentDsBlock)
        {
            var childDsBlocks = new List<DsBlockBase>();

            var measurementDsBlock = (Technology.MeasurementDsBlock)CreateDsBlock(Technology_Measurement_DsBlockType, "M", componentDsBlock.ParentModule, componentDsBlock);
            childDsBlocks.Add(measurementDsBlock);

            var analogInputDsBlock = (Pcs.AnalogInputDsBlock)CreateDsBlock(Pcs_AnalogInput_DsBlockType, "AI", componentDsBlock.ParentModule, componentDsBlock);
            analogInputDsBlock.PRIMARY_MEASUREMENT_VALUE.Connection = DsConnectionsFactory.CreateRefConnection(@"M.PRIMARY_MEASUREMENT_VALUE", componentDsBlock.ParentModule, componentDsBlock);
            analogInputDsBlock.PRIMARY_MEASUREMENT_STATUS.Connection = DsConnectionsFactory.CreateRefConnection(@"M.PRIMARY_MEASUREMENT_STATUS", componentDsBlock.ParentModule, componentDsBlock);            
            childDsBlocks.Add(analogInputDsBlock);

            var journalDsBlock = (JournalDsBlock)CreateDsBlock(Journal_DsBlockType, "PV_JOURNAL", componentDsBlock.ParentModule, componentDsBlock);
            journalDsBlock.VALUE.Connection = DsConnectionsFactory.CreateRefConnection(@"M.PRIMARY_MEASUREMENT_VALUE", componentDsBlock.ParentModule, componentDsBlock);
            journalDsBlock.HI_LIM.Connection = DsConnectionsFactory.CreateRefConnection(@"M.SENSOR_HI_LIM", componentDsBlock.ParentModule, componentDsBlock);
            journalDsBlock.LO_LIM.Connection = DsConnectionsFactory.CreateRefConnection(@"M.SENSOR_LO_LIM", componentDsBlock.ParentModule, componentDsBlock);
            childDsBlocks.Add(journalDsBlock);

            componentDsBlock.ChildDsBlocks = childDsBlocks.ToArray();

            componentDsBlock.ParamAliases = new[]
            {
                new DsParamAlias(@"PV", @"AI.MEASUREMENT_VALUE", componentDsBlock),
                new DsParamAlias(@"UNITS", @"AI.UNITS", componentDsBlock),
                new DsParamAlias(@"MAX", @"M.SENSOR_HI_LIM", componentDsBlock),
                new DsParamAlias(@"MIN", @"M.SENSOR_LO_LIM", componentDsBlock),
                new DsParamAlias(@"HH", @"AI.HIGH_HIGH_ALARM_LIMIT", componentDsBlock),
                new DsParamAlias(@"H", @"AI.HIGH_ALARM_LIMIT", componentDsBlock),
                new DsParamAlias(@"L", @"AI.LOW_ALARM_LIMIT", componentDsBlock),
                new DsParamAlias(@"LL", @"AI.LOW_LOW_ALARM_LIMIT", componentDsBlock),                
            };

            componentDsBlock.JournalParamAliases = new[]
            {
                new DsParamAlias(@"PV", "PV_JOURNAL.VALUE", componentDsBlock)                
            };
        }

        private static void CreatePIDTemplateComponentDsBlock(ComponentDsBlock componentDsBlock)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
