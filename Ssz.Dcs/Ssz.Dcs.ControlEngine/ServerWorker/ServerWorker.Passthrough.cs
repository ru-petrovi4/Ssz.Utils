using Grpc.Core;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using Ssz.Dcs.CentralServer.Common;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Ssz.Dcs.ControlEngine
{
    public partial class ServerWorker : DataAccessServerWorkerBase
    {
        #region public functions
        
        public override Task<ReadOnlyMemory<byte>> PassthroughAsync(ServerContext serverContext, string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            try
            {
                switch (passthroughName)
                {
                    case PassthroughConstants.Shutdown:                        
                        OnShutdownRequested();
                        return Task.FromResult(ReadOnlyMemory<byte>.Empty);
                    default:
                        throw new RpcException(new Status(StatusCode.InvalidArgument, "Unknown passthroughName."));
                }
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Exception during passthrough."), ex.Message);
            }
        }

        public override string LongrunningPassthrough(ServerContext serverContext, string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            string jobId = Guid.NewGuid().ToString();
            try
            {
                switch (passthroughName)
                {
                    case LongrunningPassthroughConstants.SaveStateFile:
                        OnSaveStateFile_LongrunningPassthrough(serverContext, jobId, Encoding.UTF8.GetString(dataToSend.Span));
                        break;
                    case LongrunningPassthroughConstants.LoadStateFile:
                        OnLoadStateFile_LongrunningPassthrough(serverContext, jobId, Encoding.UTF8.GetString(dataToSend.Span));
                        break;
                    case LongrunningPassthroughConstants.Step:
                        OnStep_LongrunningPassthrough(serverContext, jobId, Encoding.UTF8.GetString(dataToSend.Span));
                        break;
                    default:
                        throw new RpcException(new Status(StatusCode.InvalidArgument, "Unknown passthroughName."));
                }

                return jobId;
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Exception during passthrough."), ex.Message);
            }
        }        

        public override void LongrunningPassthroughCancel(ServerContext serverContext, string jobId)
        {   
        }

        #endregion

        #region private functions

        private async void OnSaveStateFile_LongrunningPassthrough(ServerContext serverContext, string jobId, string arg)
        {
            if (Device is null) throw new RpcException(new Status(StatusCode.InvalidArgument, "Device is not created yet."));

            try
            {
                await Device.SaveStateAsync(arg + DsFilesStoreConstants.ControlEngineStateFileExtension);

                serverContext.AddCallbackMessage(new LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    StatusCode = StatusCodes.Good
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "SaveState Failed.");
                serverContext.AddCallbackMessage(new LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    StatusCode = StatusCodes.BadInvalidArgument
                });
            }
        }

        private async void OnLoadStateFile_LongrunningPassthrough(ServerContext serverContext, string jobId, string arg)
        {
            if (Device is null) throw new RpcException(new Status(StatusCode.InvalidArgument, "Device is not created yet."));

            try
            {                
                foreach (ServerContext sc in ServerContextsDictionary.Values.ToArray())
                {
                    foreach (var list in sc.Lists)
                    {
                        list.ResetList();
                    }                   
                }

                await Device.LoadStateAsync(arg + DsFilesStoreConstants.ControlEngineStateFileExtension);                

                serverContext.AddCallbackMessage(new LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    StatusCode = StatusCodes.Good
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "LoadState Failed.");
                serverContext.AddCallbackMessage(new LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    StatusCode = StatusCodes.BadInvalidArgument
                });
            }            
        }        

        private async void OnStep_LongrunningPassthrough(ServerContext serverContext, string jobId, string arg)
        {
            if (Device is null) throw new RpcException(new Status(StatusCode.InvalidArgument, "Device is not created yet."));

            try
            {
                await Device.StepAsync(new Any(arg).ValueAsUInt32(false));

                serverContext.AddCallbackMessage(new LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    StatusCode = StatusCodes.Good
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Step Failed.");
                serverContext.AddCallbackMessage(new LongrunningPassthroughCallbackMessage
                {
                    JobId = jobId,
                    ProgressPercent = 100,
                    StatusCode = StatusCodes.BadInvalidArgument
                });
            }            
        }

        #endregion
    }
}