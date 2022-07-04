using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Xi.Client.Api.ListItems;
using Ssz.Xi.Client.Api.Lists;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    public class XiEventListItemsManager
    {
        #region construction and destruction

        public XiEventListItemsManager(IDataAccessProvider dataAccessProvider)
        {
            _dataAccessProvider = dataAccessProvider;
        }

        #endregion

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
                var kvp in _eventMessagesCallbackEventHandlers)
            {
                if (ct.IsCancellationRequested) return;
                if (kvp.Value.P is not null) continue;

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
                        Action<IDataAccessProvider, Ssz.Utils.DataAccess.EventMessagesCollection> eventMessagesCallbackEventHandler = kvp.Key;

                        xiEventList.EventMessagesCallbackEvent +=
                            (IXiEventListProxy eventList, IEnumerable<IXiEventListItem> newListItems) =>
                            {
                                if (ct.IsCancellationRequested) return;
                                if (сallbackDoer is not null)
                                {
                                    try
                                    {
                                        сallbackDoer.BeginInvoke(ct =>
                                        {
                                            EventMessagesCollection eventMessagesCollection = new();
                                            eventMessagesCollection.EventMessages = newListItems.Select(li => li.EventMessage.ToEventMessage()).ToList();
                                            eventMessagesCallbackEventHandler(_dataAccessProvider, eventMessagesCollection);
                                        });
                                    }
                                    catch (Exception)
                                    {
                                    }
                                }
                            };

                        if (callbackable)
                        {
                            try
                            {
                                xiEventList.Callbackable = true;
                            }
                            catch
                            {
                            }                            
                        }
                        try
                        {
                            xiEventList.Pollable = true;
                        }
                        catch
                        {
                        }
                        try
                        {
                            xiEventList.Writeable = true;
                        }
                        catch
                        {
                        }

                        xiEventList.EnableListUpdating(true);
                    }
                    catch (Exception)
                    {
                        allOk = false;
                    }
                }
                catch
                {
                    //Logger?.LogWarning(ex);
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
                var kvp in _eventMessagesCallbackEventHandlers)
            {
                IXiEventListProxy? xiEventList = _eventMessagesCallbackEventHandlers[kvp.Key].P;

                if (xiEventList is null || xiEventList.Disposed) continue;
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

        public Utils.DataAccess.EventMessagesCollection? ReadEventMessagesJournal(DateTime firstTimestampUtc, DateTime secondTimestampUtc, CaseInsensitiveDictionary<string?>? params_)
        {
            return null;
        }

        /// <summary>
        ///     Invokes EventList.PollEventChanges(null) if EventList Pollable and not Callbackable.
        ///     No throw.
        /// </summary>
        public void PollChangesIfNotCallbackable()
        {
            foreach (
                var kvp in _eventMessagesCallbackEventHandlers)
            {
                IXiEventListProxy? xiEventList = _eventMessagesCallbackEventHandlers[kvp.Key].P;

                if (xiEventList is null || xiEventList.Disposed) continue;
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
                var kvp in _eventMessagesCallbackEventHandlers)
            {
                IXiEventListProxy? xiEventList = _eventMessagesCallbackEventHandlers[kvp.Key].P;

                if (xiEventList is not null)
                    xiEventList.Dispose();

                _eventMessagesCallbackEventHandlers[kvp.Key].P = null;
            }

            _xiEventItemsMustBeAdded = true;
        }

        public IXiEventListProxy? GetRelatedXiEventList(Action<IDataAccessProvider, EventMessagesCollection> eventHandler)
        {
            XiEventListPointer? xiEventListPointer;
            if (!_eventMessagesCallbackEventHandlers.TryGetValue(eventHandler, out xiEventListPointer)) return null;
            return xiEventListPointer.P;
        }

        public event Action<IDataAccessProvider, EventMessagesCollection> EventMessagesCallback
        {
            add
            {
                _eventMessagesCallbackEventHandlers.Add(value, new XiEventListPointer());
                _xiEventItemsMustBeAdded = true;
            }
            remove
            {
                XiEventListPointer? xiEventListPointer;
                if (!_eventMessagesCallbackEventHandlers.TryGetValue(value, out xiEventListPointer)) return;
                _eventMessagesCallbackEventHandlers.Remove(value);
                if (xiEventListPointer.P is not null)
                {
                    try
                    {
                        xiEventListPointer.P.Dispose();
                    }
                    catch
                    {
                        //Logger?.LogWarning(ex);
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

        private readonly Dictionary<Action<IDataAccessProvider, EventMessagesCollection>, XiEventListPointer> _eventMessagesCallbackEventHandlers =
            new();

        private string _xiSystem = "";
        private IDataAccessProvider _dataAccessProvider;        

        #endregion

        private class XiEventListPointer
        {
            #region public functions

            public IXiEventListProxy? P;

            #endregion
        }
    }
}