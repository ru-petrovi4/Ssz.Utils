using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.Client.LocalServer
{
    public class DuplexStream<T> : IServerStreamWriter<T>, IAsyncStreamReader<T>
    {
        #region public functions    

        public WriteOptions? WriteOptions { get; set; }

        public async Task WriteAsync(T message)
        {
            await _channel.Writer.WriteAsync(message);
        }

        public void Complete()
        {            
            _channel.Writer.Complete();
        }

        public T Current => _current!;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            try
            {
                return await _channel.Reader.WaitToReadAsync(cancellationToken) &&
                       _channel.Reader.TryRead(out _current);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
        }        

        #endregion       

        #region private fields

        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();        
        private T? _current;

        #endregion
    }
}


//public class DuplexStream<T> : IServerStreamWriter<T>, IAsyncStreamReader<T>
//{
//    #region public functions    

//    public WriteOptions? WriteOptions { get; set; }

//    public async Task WriteAsync(T message)
//    {
//        if (_isCompleted)
//            throw new InvalidOperationException("Cannot write to a completed stream.");

//        await _channel.Writer.WriteAsync(message);
//    }

//    public void Complete()
//    {
//        _isCompleted = true;
//        _channel.Writer.Complete();
//    }

//    public T Current => _current!;

//    public async Task<bool> MoveNext(CancellationToken cancellationToken)
//    {
//        if (_isCompleted)
//            return false;

//        try
//        {
//            return await _channel.Reader.WaitToReadAsync(cancellationToken) &&
//                   _channel.Reader.TryRead(out _current);
//        }
//        catch (OperationCanceledException)
//        {
//            return false;
//        }
//    }

//    #endregion

//    #region private fields

//    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
//    private bool _isCompleted;
//    private T? _current;

//    #endregion
//}