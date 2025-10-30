using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.DataAccess
{
    public class EventMessagesCollection
    {
        public List<EventMessage> EventMessages { get; set; } = new();

        public CaseInsensitiveOrderedDictionary<string?>? CommonFields { get; set; }
    }
}
