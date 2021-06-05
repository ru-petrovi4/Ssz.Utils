using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess.EventSourceModel
{
    public static class EventSourceModelHelper
    {
        #region public functions

        //public static void FillInEventSourceModel(Ssz.Utils.EventSourceModel.EventSourceModel eventSourceModel, 
        //    CaseInsensitiveDictionary<List<string?>> data, int areasColumnIndex)
        //{
        //    foreach (var line in data.Values)
        //    {

        //    }
        //}

        public static void AddTagToEventSourceModel(Ssz.Utils.EventSourceModel.EventSourceModel eventSourceModel,
            string tag, string areas)
        {
            Ssz.Utils.EventSourceModel.EventSourceObject eventSourceObject = eventSourceModel.GetEventSourceObject(tag);
            if (areas != @"")
            {
                string area = @"";
                foreach (string areaPart in areas.Split('\\', '/'))
                {
                    if (area == @"") area = areaPart;
                    else area += "/" + areaPart;
                    eventSourceObject.EventSourceAreas[area] = eventSourceModel.GetEventSourceArea(area);                    
                }
            }
        }

        #endregion
    }
}
