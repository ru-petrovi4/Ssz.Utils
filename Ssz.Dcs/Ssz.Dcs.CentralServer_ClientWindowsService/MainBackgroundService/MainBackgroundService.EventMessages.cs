using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ssz.Dcs.CentralServer.Common;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer_ClientWindowsService
{
    public partial class MainBackgroundService : BackgroundService
    {
        #region private functions               

        private void OnUtilityDataAccessProvider_EventMessagesCallback(object? sender, EventMessagesCallbackEventArgs args)
        {
            foreach (var eventMessage in args.EventMessagesCollection.EventMessages)
            {
                Logger.LogDebug("eventMessage: " + eventMessage.TextMessage);

                if (eventMessage.EventType != EventType.SystemEvent) 
                    continue;
                if (eventMessage.EventId is null || eventMessage.EventId.Conditions is null ||
                        eventMessage.TextMessage == @"") 
                    continue;
                var condition = eventMessage.EventId.Conditions.FirstOrDefault();
                if (condition is null) 
                    continue;

                if (condition.Compare(EventMessageConstants.LaunchInstructor_TypeId))
                {
                    Logger.LogDebug("eventMessage, LaunchInstructor_TypeId: " + eventMessage.TextMessage);

                    _threadSafeDispatcher.BeginAsyncInvoke(async ct =>
                    {
                        await LaunchInstructorAsync(eventMessage.TextMessage);
                    });
                }
                else if (condition.Compare(EventMessageConstants.LaunchEngine_TypeId))
                {
                    Logger.LogDebug("eventMessage, LaunchEngine_TypeId: " + eventMessage.TextMessage);

                    _threadSafeDispatcher.BeginAsyncInvoke(async ct =>
                    {
                        await LaunchEnineAsync(eventMessage.TextMessage);
                    });
                }
                else if (condition.Compare(EventMessageConstants.LaunchOperator_TypeId))
                {
                    Logger.LogDebug("eventMessage, PrepareLaunchOperator_TypeId: " + eventMessage.TextMessage);

                    _threadSafeDispatcher.BeginAsyncInvoke(async ct =>
                    {
                        await LaunchOperatorAsync(eventMessage.TextMessage);
                    });
                }
                else if (condition.Compare(EventMessageConstants.DownloadChangedFiles_TypeId))
                {
                    Logger.LogDebug("eventMessage, DownloadChangedFiles_TypeId: " + eventMessage.TextMessage);

                    _threadSafeDispatcher.BeginAsyncInvoke(async ct =>
                    {
                        await DownloadChangedFilesAsync(eventMessage.TextMessage);
                    });
                }
                else if (condition.Compare(EventMessageConstants.UploadChangedFiles_TypeId))
                {
                    Logger.LogDebug("eventMessage, UploadChangedFiles_TypeId: " + eventMessage.TextMessage);

                    _threadSafeDispatcher.BeginAsyncInvoke(async ct =>
                    {
                        await UploadChangedFilesAsync(eventMessage.TextMessage);
                    });
                }
            }
        }

        #endregion
    }
}
