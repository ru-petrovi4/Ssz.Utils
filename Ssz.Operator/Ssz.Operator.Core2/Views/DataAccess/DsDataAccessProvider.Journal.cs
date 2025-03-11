using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ssz.Operator.Core.Utils;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils.DataAccess;

namespace Ssz.Operator.Core.DataAccess
{
    public partial class DsDataAccessProvider : GrpcDataAccessProvider
    {
        #region public functions

        public async Task<IEnumerable<ValueStatusTimestamp>> ReadElementValuesJournal(string elementId, DateTime firstTimestampUtc, DateTime secondTimestampUtc)
        {            
            if (!IsInitialized)
                return new ValueStatusTimestamp[0];

            List<string?>? mapValues = null;

            if (ElementIdsMap is not null)
            {
                var constAny = Ssz.Utils.ElementIdsMap.TryGetConstValue(elementId);
                if (!constAny.HasValue)
                {
                    mapValues = ElementIdsMap.GetFromMap(elementId);

                    if (mapValues is not null && mapValues.Skip(2).All(v => String.IsNullOrEmpty(v)))
                    {
                        constAny = Ssz.Utils.ElementIdsMap.TryGetConstValue(mapValues[1]);
                    }
                }

                if (constAny.HasValue)
                {
                    ValueStatusTimestamp[] values;
                    if (constAny.Value.ValueTypeCode == Ssz.Utils.Any.TypeCode.Empty || constAny.Value.ValueTypeCode == Ssz.Utils.Any.TypeCode.DBNull)
                        values = new ValueStatusTimestamp[0];
                    else
                        values = new ValueStatusTimestamp[2]
                        {
                            new() {Value = constAny.Value, TimestampUtc = firstTimestampUtc},
                            new() {Value = constAny.Value, TimestampUtc = secondTimestampUtc}
                        };

                    return values;
                }
            }
            
            if (mapValues is not null)
            { 
                var taskCompletionSource = new TaskCompletionSource<IEnumerable<ValueStatusTimestamp>>();

                WorkingThreadSafeDispatcher.BeginInvokeEx(async ct =>
                {
                    try
                    {
                        var result = await ReadElementValuesJournalInternalAsync(mapValues[1] ?? "", firstTimestampUtc, secondTimestampUtc);

                        taskCompletionSource.SetResult(result.ToValueStatusTimestams());
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }
                });                    

                return await taskCompletionSource.Task;
            }
            else
            {
                var taskCompletionSource = new TaskCompletionSource<IEnumerable<ValueStatusTimestamp>>();

                WorkingThreadSafeDispatcher.BeginInvokeEx(async ct =>
                {
                    try
                    {
                        var result = await ReadElementValuesJournalInternalAsync(elementId, firstTimestampUtc, secondTimestampUtc);

                        taskCompletionSource.SetResult(result.ToValueStatusTimestams());
                    }
                    catch (Exception ex)
                    {
                        taskCompletionSource.SetException(ex);
                    }                    
                });

                return await taskCompletionSource.Task;
            }
        }

        #endregion
    }
}