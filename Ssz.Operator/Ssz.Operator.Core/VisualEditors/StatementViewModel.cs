


using Ssz.Operator.Core.MultiValueConverters;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.VisualEditors
{
    public class StatementViewModel : ViewModelBase
    {
        #region private fields

        private DsXaml? _constXaml;

        #endregion

        #region construction and destruction

        public StatementViewModel(TextStatement? value)
        {
            if (value is null)
            {
                Condition = new Expression();
                Value = new Expression();
                ParamNum = 0;
            }
            else
            {
                Condition = new Expression(value.Condition);
                Value = new Expression(value.Value);
                ParamNum = value.ParamNum;
            }
        }

        public StatementViewModel(DsBrushStatement? value)
        {
            if (value is null)
            {
                Condition = new Expression();
                ConstDsBrush = null;
                ParamNum = null;
            }
            else
            {
                Condition = new Expression(value.Condition);
                if (value.ConstDsBrush is not null) ConstDsBrush = (DsBrushBase) value.ConstDsBrush.Clone();
                ParamNum = value.ParamNum;
            }
        }

        public StatementViewModel(XamlStatement? value)
        {
            if (value is null)
            {
                Condition = new Expression();
                ConstXaml = new DsXaml();
            }
            else
            {
                Condition = new Expression(value.Condition);
                var constXaml = (DsXaml) value.ConstXaml;
                ConstXaml = (DsXaml) constXaml.Clone();
                ConstXaml.ParentItem = constXaml.ParentItem;
            }
        }

        #endregion

        #region public functions

        public Expression Condition { get; set; }

        public Expression? Value { get; set; }

        public DsXaml? ConstXaml
        {
            get => _constXaml;
            set => SetValue(ref _constXaml, value);
        }

        public DsBrushBase? ConstDsBrush { get; set; }

        public int? ParamNum { get; set; }

        public object? ConstDsBrushOrParamNum
        {
            get
            {
                if (ParamNum.HasValue)
                    return ParamNum.Value;
                return ConstDsBrush;
            }
            set
            {
                if (value is int)
                {
                    ParamNum = (int) value;
                    ConstDsBrush = null;
                }
                else
                {
                    ParamNum = null;
                    ConstDsBrush = value as DsBrushBase;
                }

                OnPropertyChangedAuto();
            }
        }

        #endregion
    }
}