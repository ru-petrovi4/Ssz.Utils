using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    public static class MessagesExtensions
    {
        public static List<ElementValuesCallback> SplitForCorrectGrpcMessageSize(this ElementValuesCallbackMessage elementValuesCallbackMessage)
        {
            byte[] fullElementValuesCollection;
            using (var memoryStream = new MemoryStream(1024))
            {
                using (var writer = new SerializationWriter(memoryStream))
                {
                    using (writer.EnterBlock(1))
                    {
                        writer.Write(elementValuesCallbackMessage.ElementValues.Count);
                        foreach (var kvp in elementValuesCallbackMessage.ElementValues)
                        {
                            uint serverAlias = kvp.Key;
                            ValueStatusTimestamp valueStatusTimestamp = kvp.Value;

                            writer.Write(serverAlias);
                            valueStatusTimestamp.SerializeOwnedData(writer, null);
                        }
                    }
                }
                fullElementValuesCollection = memoryStream.ToArray();
            }

            List<ElementValuesCallback> list = new();
            foreach (DataChunk elementValuesCollection in ProtobufHelper.SplitForCorrectGrpcMessageSize(fullElementValuesCollection))
            {
                var elementValuesCallback = new ElementValuesCallback();
                elementValuesCallback.ListClientAlias = elementValuesCallbackMessage.ListClientAlias;
                elementValuesCallback.ElementValuesCollection = elementValuesCollection;
                list.Add(elementValuesCallback);
            }
            return list;
        }

        public static List<EventMessagesCallback> SplitForCorrectGrpcMessageSize(this EventMessagesCallbackMessage eventMessagesCallbackMessage)
        {
            List<EventMessagesCallback> result = new();
            foreach (EventMessagesCollection eventMessagesCollection in ProtobufHelper.SplitForCorrectGrpcMessageSize(
                eventMessagesCallbackMessage.EventMessages.Select(em => new EventMessage(em)).ToList(), 
                eventMessagesCallbackMessage.CommonFields))
            {
                var eventMessagesCallback = new EventMessagesCallback();
                eventMessagesCallback.ListClientAlias = eventMessagesCallbackMessage.ListClientAlias;
                eventMessagesCallback.EventMessagesCollection = eventMessagesCollection;
                result.Add(eventMessagesCallback);
            }
            return result;
        }
    }
}
