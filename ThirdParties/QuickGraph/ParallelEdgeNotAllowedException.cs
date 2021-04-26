using System;

namespace QuickGraph
{
#if !SILVERLIGHT
    [Serializable]
#endif
    public class ParallelEdgeNotAllowedException 
        : QuickGraphException
    {
        public ParallelEdgeNotAllowedException() { }
        public ParallelEdgeNotAllowedException(string message) : base( message ) { }
        public ParallelEdgeNotAllowedException(string message, System.Exception inner) : base( message, inner ) { }
#if !SILVERLIGHT
        protected ParallelEdgeNotAllowedException(
          System.Play.Serialization.SerializationInfo info,
          System.Play.Serialization.StreamingContext context) : base( info, context ) { }
#endif
    }
}
