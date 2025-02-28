using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils
{    
    public class DummyDispatcher : IDispatcher
    {
        #region public functions

        public void BeginInvoke(Action<CancellationToken> action)
        {
            try
            {
                action(CancellationToken.None);
            }
            catch
            {
            }
        }

        public async void BeginInvokeEx(Func<CancellationToken, Task> action)
        {
            try
            {
                await action(CancellationToken.None);
            }
            catch
            {
            }
        }        

        #endregion
    }
}


///// <summary>
/////     Uses SynchronizationContext for doing work.
///// </summary>
//public class DefaultDispatcher : IDispatcher
//{
//    #region construction and destruction

//    public DefaultDispatcher()
//    {
//        _synchronizationContext = SynchronizationContext.Current;
//    }

//    #endregion

//    #region public functions

//    public void BeginInvoke(Action<CancellationToken> action)
//    {
//        _synchronizationContext?.Post(state =>
//        {
//            try
//            {
//                action(CancellationToken.None);
//            }
//            catch
//            {
//            }
//        }, null);
//    }

//    public void BeginInvokeEx(Func<CancellationToken, Task> action)
//    {
//        _synchronizationContext?.Post(async state =>
//        {
//            try
//            {
//                await action(CancellationToken.None);
//            }
//            catch
//            {
//            }
//        }, null);
//    }

//    #endregion

//    #region private fields

//    private readonly SynchronizationContext? _synchronizationContext;

//    #endregion
//}