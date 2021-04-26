using System;
using System.Collections.Generic;
using System.Text;

namespace QuickGraph
{    
#if !SILVERLIGHT
    [Serializable]
#endif
    public class NegativeCycleGraphException
        : QuickGraphException
    {
        public NegativeCycleGraphException() { }
        public NegativeCycleGraphException(string message) : base(message) { }
        public NegativeCycleGraphException(string message, Exception inner) : base(message, inner) { }
#if !SILVERLIGHT
        protected NegativeCycleGraphException(
          System.Play.Serialization.SerializationInfo info,
          System.Play.Serialization.StreamingContext context)
            : base(info, context) { }
#endif
    }
}
