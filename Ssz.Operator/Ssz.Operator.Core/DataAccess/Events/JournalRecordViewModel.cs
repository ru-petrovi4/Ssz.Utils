using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Windows.Media;

namespace Ssz.Operator.Core.DataAccess
{    
    public class JournalRecordViewModel : ViewModelBase
    {
        #region public functions

        public int EventSubType { get; set; }

        public DateTime OccurrenceTimeUtc { get; set; }

        public UInt64 ProcessModelTimeSeconds { get; set; }

        public string OperatorRoleId { get; set; } = @"";

        public string OperatorRoleName { get; set; } = @"";

        public string OperatorUserName { get; set; } = @"";        

        public string Tag
        {
            get { return _tag; }
            set { SetValue(ref _tag, value); }
        }

        public string Description
        {
            get { return _description; }
            set { SetValue(ref _description, value); }
        }

        public string PvFormat
        {
            get { return _pvFormat; }
            set { SetValue(ref _pvFormat, value); }
        }

        public string Property { get; set; } = @"";

        public Any OldValue { get; set; }

        public Any NewValue { get; set; }

        #endregion

        #region private functions

        private string _tag = @"";

        private string _description = @"";

        private string _pvFormat = @"";

        #endregion
    }
}
