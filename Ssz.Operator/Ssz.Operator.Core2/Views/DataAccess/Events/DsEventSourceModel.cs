using System;
using System.Collections.Generic;
using System.Linq;

using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Utils;
using Ssz.Utils.DataAccess;

namespace Ssz.Operator.Core.DataAccess
{
    public class DsEventSourceModel : EventSourceModel
    {
        #region public functions  

        public const int AlarmBrush_SubscriptionType = 0x5;

        /// <summary>        
        ///     Empty area is for root Area.
        /// </summary>
        /// <param name="area"></param>
        /// <returns></returns>        
        public override EventSourceArea GetOrCreateEventSourceArea(string area)
        {
            EventSourceArea? eventSourceArea;
            if (!EventSourceAreas.TryGetValue(area, out eventSourceArea))
            {
                eventSourceArea = new DsEventSourceArea(area, DataAccessProvider);
                EventSourceAreas[area] = eventSourceArea;
            }
            return eventSourceArea;
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="tagName"></param>
        /// <param name="area">Can be compound area in format 'ROOT_AREA/CHILD_AREA'</param>
        /// <returns></returns>
        public override EventSourceObject GetOrCreateEventSourceObject(string tagName, string? area = null)
        {
            EventSourceObject? existingEventSourceObject;
            if (EventSourceObjects.TryGetValue(tagName, out existingEventSourceObject))
                //We already have this tag in our list.  Just return the existing object.
                return existingEventSourceObject;

            //The tag doesn't already exist.  Create a new EventSourceObject and add it to our dictionary
            var newEventSourceObject = new DsEventSourceObject(tagName, DataAccessProvider);
            EventSourceObjects[tagName] = newEventSourceObject;

            EventSourceArea overviewEventSourceArea = GetOrCreateEventSourceArea(@"");
            newEventSourceObject.EventSourceAreas[@""] = overviewEventSourceArea;

            var dsConstants = DsProject.Instance.AllDsPagesCacheGetAllConstantsValues().TryGetValue(tagName);
            if (dsConstants is not null)
            {
                IEnumerable<DsPageDrawing> dsPageDrawings = dsConstants
                    .Select(i =>
                        (DsPageDrawing)(i.ComplexDsShape.GetParentDrawing()!))
                    .Distinct(ReferenceEqualityComparer<DsPageDrawing>.Default);

                foreach (DsPageDrawing dsPageDrawing in dsPageDrawings)
                {                    
                    newEventSourceObject.EventSourceAreas[dsPageDrawing.Name] = (DsEventSourceArea)GetOrCreateEventSourceArea(dsPageDrawing.Name);

                    if (!String.IsNullOrEmpty(dsPageDrawing.Group))
                    {
                        string currentArea = @"";
                        foreach (string areaPart in dsPageDrawing.Group!.Split('/'))
                        {
                            if (currentArea == @"") currentArea = areaPart;
                            else currentArea += "/" + areaPart;
                            newEventSourceObject.EventSourceAreas[currentArea] = GetOrCreateEventSourceArea(currentArea);
                        }                        
                    }
                }
            }

            if (!String.IsNullOrEmpty(area))
            {
                string currentArea = @"";
                foreach (string areaPart in area!.Split('/'))
                {
                    if (currentArea == @"") currentArea = areaPart;
                    else currentArea += "/" + areaPart;
                    newEventSourceObject.EventSourceAreas[currentArea] = GetOrCreateEventSourceArea(currentArea);
                }
            }

            return newEventSourceObject;
        }        

        public override IEnumerable<AlarmInfoViewModelBase> GetExistingAlarmInfoViewModels()
        {
            var alarmInfoViewModels = new List<AlarmInfoViewModelBase>();

            foreach (var kvp in EventSourceObjects)
            {
                EventSourceObject eventSourceObject = kvp.Value;
                var alarmInfoViewModelsForObject = new List<AlarmInfoViewModelBase>();
                foreach (var condition in eventSourceObject.AlarmConditions.Values.OrderByDescending(
                    cs => cs.CategoryId))
                {
                    if (!condition.Active && condition.Unacked &&
                        condition.LastAlarmInfoViewModel is not null)
                        alarmInfoViewModelsForObject.Add(condition.LastAlarmInfoViewModel);
                }
                foreach (var condition in eventSourceObject.AlarmConditions.Values.OrderBy(cs => cs.CategoryId))
                {
                    if (condition.Active &&
                        condition.LastAlarmInfoViewModel is not null)
                        alarmInfoViewModelsForObject.Add(condition.LastAlarmInfoViewModel);
                }
                if (eventSourceObject.NormalConditionState.Active && eventSourceObject.NormalConditionState.Unacked &&
                    eventSourceObject.NormalConditionState.LastAlarmInfoViewModel is not null)
                    alarmInfoViewModelsForObject.Add(eventSourceObject.NormalConditionState.LastAlarmInfoViewModel);
                alarmInfoViewModels.AddRange(alarmInfoViewModelsForObject);
            }

            return alarmInfoViewModels;
        }

        public void AddSubscription(int subscriptionType, string subscriptionParams, IValueSubscription valueSubscription)
        {
            var parts = CsvHelper.ParseCsvLine(",", subscriptionParams);
            string tagOrAreaOrDsPageName = parts[0] ?? @"";            
            var subscriptionScope = EventSourceModelSubscriptionScope.Active;
            if (parts.Length > 1 && !String.IsNullOrEmpty(parts[1]))
                subscriptionScope = new Any(parts[1]!).ValueAs<EventSourceModelSubscriptionScope>(false);
            uint? alarmCategoryIdFilter = null;
            if (parts.Length > 2 && parts[2] != null && parts[2] != @"")
                alarmCategoryIdFilter = new Any(parts[2] ?? @"").ValueAsUInt32(false);

            if (string.IsNullOrEmpty(tagOrAreaOrDsPageName))
            {
                EventSourceArea eventSourceArea = GetOrCreateEventSourceArea(@"");
                eventSourceArea.Subscriptions.Add(valueSubscription,
                    new EventSourceModelSubscriptionInfo(subscriptionType, subscriptionScope, alarmCategoryIdFilter));
                eventSourceArea.NotifySubscriptions();
                return;
            }

            CaseInsensitiveDictionary<DsPageDrawing> dsPageDrawings =
                DsProject.Instance.AllDsPagesCache;
            if (dsPageDrawings.ContainsKey(tagOrAreaOrDsPageName))
            {
                var eventSourceArea = (DsEventSourceArea)GetOrCreateEventSourceArea(tagOrAreaOrDsPageName);                
                eventSourceArea.Subscriptions.Add(valueSubscription,
                    new EventSourceModelSubscriptionInfo(subscriptionType, subscriptionScope, alarmCategoryIdFilter));
                eventSourceArea.NotifySubscriptions();                
            }
            else if (tagOrAreaOrDsPageName.StartsWith("AREA:", StringComparison.InvariantCultureIgnoreCase))
            {
                tagOrAreaOrDsPageName = tagOrAreaOrDsPageName.Substring("AREA:".Length);
                var eventSourceArea = (DsEventSourceArea)GetOrCreateEventSourceArea(tagOrAreaOrDsPageName);
                eventSourceArea.Subscriptions.Add(valueSubscription,
                    new EventSourceModelSubscriptionInfo(subscriptionType, subscriptionScope, alarmCategoryIdFilter));
                eventSourceArea.NotifySubscriptions();
            }
            else
            {
                EventSourceObject eventSourceObject = GetOrCreateEventSourceObject(tagOrAreaOrDsPageName);
                var eventSourceModelSubscriptionInfo = new EventSourceModelSubscriptionInfo(subscriptionType, subscriptionScope, alarmCategoryIdFilter);
                eventSourceObject.Subscriptions.Add(valueSubscription, eventSourceModelSubscriptionInfo);
                eventSourceObject.NotifySubscription(valueSubscription, eventSourceModelSubscriptionInfo);
            }            
        }

        public void RemoveSubscription(string subscriptionParams, IValueSubscription valueSubscription)
        {
            var parts = CsvHelper.ParseCsvLine(",", subscriptionParams);
            string tagOrAreaOrDsPageName = parts[0] ?? @"";

            if (string.IsNullOrEmpty(tagOrAreaOrDsPageName))
            {
                EventSourceArea eventSourceArea = GetOrCreateEventSourceArea(@"");
                eventSourceArea.Subscriptions.Remove(valueSubscription);
                return;
            }

            CaseInsensitiveDictionary<DsPageDrawing> dsPageDrawings =
                DsProject.Instance.AllDsPagesCache;
            if (dsPageDrawings.ContainsKey(tagOrAreaOrDsPageName))
            {
                EventSourceArea eventSourceArea = GetOrCreateEventSourceArea(tagOrAreaOrDsPageName);                
                eventSourceArea.Subscriptions.Remove(valueSubscription);
                return;
            }

            EventSourceObject eventSourceObject = GetOrCreateEventSourceObject(tagOrAreaOrDsPageName);
            eventSourceObject.Subscriptions.Remove(valueSubscription);
        }

        #endregion
    }
}