using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DataEngines
{
    public class GenericDataEngine : DataEngineBase
    {
        #region public functions

        public static GenericDataEngine Instance { get; } = new();

        public static readonly Guid DataEngineGuid = new(@"BC85A433-8B8C-480A-970D-7AFDC3B9CCDE");

        public override Guid Guid => DataEngineGuid;

        public override string NameToDisplay => Resources.GenericDataEngine_NameToDisplay;

        public override string Description => Resources.GenericDataEngine_Description;
        
        #endregion                
    }
}