using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ssz.Utils;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    public class XiEventListItemsManager
    {
        #region public functions

        /// <summary>
        ///     No throw.
        /// </summary>
        /// <param name="xiServerProxy"></param>
        /// <param name="сallbackDoer"></param>        
        /// <param name="callbackable"></param>
        /// <param name="ct"></param>
        public void Subscribe(XiServerProxy xiServerProxy, IDispatcher? сallbackDoer, bool callbackable, CancellationToken ct)
        {
            if (ct.IsCancellationRequested) return;
            if (!xiServerProxy.ContextExists) return;
            if (!_xiEventItemsMustBeAdded) return;

            bool allOk = true;

            foreach (
                var kvp in _eventNotificationEventHandlers)
            {
                if (ct.IsCancellationRequested) return;
                if (kvp.Value.P != null) continue;

                var f = new List<ORedFilters>();
                var filters = new FilterSet
                {
                    Filters = f
                };
                var fc = new List<FilterCriterion>();
                f.Add(new ORedFilters
                {
                    FilterCriteria = fc
                });
                fc.Add(new FilterCriterion
                {
                    Operator = 1, //Eq
                    ComparisonValue =
                        XiSystem,
                    OperandName = "Area"
                });

                IXiEventListProxy xiEventList;

                try
                {
                    xiEventList = xiServerProxy.NewEventList(0, 0, filters);
                }
                catch (Exception)
                {
                    return;
                }

                try
                {
                    if (xiEventList.Disposed)
                    {
                        return;
                    }

                    try
                    {
                        Action<Ssz.Utils.DataSource.EventMessage[]> eventNotificationEventHandler = kvp.Key;

                        xiEventList.EventNotificationEvent +=
                            (IXiEventListProxy eventList, IEnumerable<IXiEventListItem> newListItems) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                if (сallbackDoer != null)
                                {
                                    try
                                    {
                                        сallbackDoer.BeginInvoke(ct => eventNotificationEventHandler(
                                            newListItems.Select(li => li.EventMessage)));
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            };

                        if (callbackable) xiEventList.Callbackable = true;
                        xiEventList.Pollable = true;                        

                        xiEventList.EnableListUpdating(true);
                    }
                    catch (Exception)
                    {
                        allOk = false;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning(ex);
                }

                kvp.Value.P = xiEventList;

                if (allOk) _xiEventItemsMustBeAdded = false;
            }
        }

        /// <summary>
        ///     If not Pollable, does nothing.
        ///     No throw.
        /// </summary>
        public void PollChanges()
        {
            foreach (
                var kvp in _eventNotificationEventHandlers)
            {
                IXiEventListProxy? xiEventList = _eventNotificationEventHandlers[kvp.Key].P;

                if (xiEventList == null || xiEventList.Disposed) continue;
                if (xiEventList.Pollable)
                {
                    try
                    {
                        xiEventList.PollEventChanges(null);
                    }
                    catch
                    {
                    }
                }   
            }            
        }

        /// <summary>
        ///     Invokes EventList.PollEventChanges(null) if EventList Pollable and not Callbackable.
        ///     No throw.
        /// </summary>
        public void PollChangesIfNotCallbackable()
        {
            foreach (
                var kvp in _eventNotificationEventHandlers)
            {
                IXiEventListProxy? xiEventList = _eventNotificationEventHandlers[kvp.Key].P;

                if (xiEventList == null || xiEventList.Disposed) continue;
                if (xiEventList.Pollable && !xiEventList.Callbackable)
                {
                    try
                    {
                        xiEventList.PollEventChanges(null);
                    }
                    catch
                    {
                    }
                }   
            }
        }

        public void Unsubscribe()
        {
            foreach (
                var kvp in _eventNotificationEventHandlers)
            {
                IXiEventListProxy? xiEventList = _eventNotificationEventHandlers[kvp.Key].P;

                if (xiEventList != null)
                    xiEventList.Dispose();

                _eventNotificationEventHandlers[kvp.Key].P = null;
            }

            _xiEventItemsMustBeAdded = true;
        }

        public IXiEventListProxy? GetRelatedXiEventList(Action<Ssz.Utils.DataSource.EventMessage[]> eventHandler)
        {
            XiEventListPointer? xiEventListPointer;
            if (!_eventNotificationEventHandlers.TryGetValue(eventHandler, out xiEventListPointer)) return null;
            return xiEventListPointer.P;
        }

        public event Action<Ssz.Utils.DataSource.EventMessage[]> EventNotification
        {
            add
            {
                _eventNotificationEventHandlers.Add(value, new XiEventListPointer());
                _xiEventItemsMustBeAdded = true;
            }
            remove
            {
                XiEventListPointer? xiEventListPointer;
                if (!_eventNotificationEventHandlers.TryGetValue(value, out xiEventListPointer)) return;
                _eventNotificationEventHandlers.Remove(value);
                if (xiEventListPointer.P != null)
                {
                    try
                    {
                        xiEventListPointer.P.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning(ex);
                    }
                }
            }
        }

        /// <summary>
        ///     Xi Alias
        /// </summary>
        public string XiSystem
        {
            get { return _xiSystem; }
            set { _xiSystem = value; }
        }

        #endregion

        #region private fields
        
        private volatile bool _xiEventItemsMustBeAdded;

        private readonly Dictionary<Action<Ssz.Utils.DataSource.EventMessage[]>, XiEventListPointer> _eventNotificationEventHandlers =
            new Dictionary<Action<Ssz.Utils.DataSource.EventMessage[]>, XiEventListPointer>();

        private string _xiSystem = "";

        #endregion

        private class XiEventListPointer
        {
            #region public functions

            public IXiEventListProxy? P;

            #endregion
        }
    }
}