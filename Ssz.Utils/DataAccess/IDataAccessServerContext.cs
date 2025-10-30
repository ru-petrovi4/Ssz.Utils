using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess;

public interface IDataAccessServerContext : IDisposable, IAsyncDisposable
{
    bool Disposed { get; }

    CancellationTokenSource CallbackWorkingTask_CancellationTokenSource { get; }

    /// <summary>
    ///   Application name handed to server when context was created.
    /// </summary>
    string ClientApplicationName { get; }

    /// <summary>
    ///   Workstation name handed to server when context was created.
    /// </summary>
    string ClientWorkstationName { get; }

    /// <summary>
    ///   The negotiated timeout in milliseconds.
    /// </summary>
    uint ContextTimeoutMs { get; }

    /// <summary>
    ///   The negotiated timeout in milliseconds.
    /// </summary>
    uint ContextStatusCallbackPeriodMs { get; }

    /// <summary>
    ///   User's culture, negotiated when context was created.
    /// </summary>
    CultureInfo CultureInfo { get; }

    string SystemNameToConnect { get; }

    CaseInsensitiveOrderedDictionary<string?> ContextParams { get; }

    event EventHandler ContextParamsChanged;

    /// <summary>
    ///    Context identifier != ""
    /// </summary>
    string ContextId { get; }        

    /// <summary>
    ///   The last time the context was accessed.
    /// </summary>
    DateTime LastAccessDateTimeUtc { get; set; }

    DateTime? LastContextStatusCallbackDateTimeUtc { get; set; }

    /// <summary>
    ///     Did the client call Conclude(...)
    /// </summary>
    bool IsConcludeCalled { get; set; }

    /// <summary>
    ///     Must be IAsyncStreamWriter CallbackMessage
    /// </summary>
    /// <param name="responseStream"></param>
    void SetResponseStream(object responseStream);

    void EnableListCallback(uint listServerAlias, ref bool isEnabled);

    void UpdateContextParams(CaseInsensitiveOrderedDictionary<string?> contextParams);

    Task<List<AliasResult>?> WriteElementValuesAsync(uint listServerAlias, ReadOnlyMemory<byte> elementValuesCollectionBytes);

    List<EventIdResult> AckAlarms(uint listServerAlias, string operatorName, string comment,
        IEnumerable<EventId> eventIdsToAck);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="listServerAlias"></param>
    /// <returns></returns>
    void TouchList(uint listServerAlias);

    Task<ReadOnlyMemory<byte>> PassthroughAsync(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend);

    string LongrunningPassthrough(string recipientPath, string passthroughName, ReadOnlyMemory<byte> dataToSend);

    void LongrunningPassthroughCancel(string jobId);

    Task<byte[]> ReadElementValuesJournalsAsync(
       uint listServerAlias,
       DateTime firstTimeStampUtc,
       DateTime secondTimeStampUtc,
       uint numValuesPerAlias,
       TypeId calculation,
       CaseInsensitiveOrderedDictionary<string?> params_,
       List<uint> serverAliases);

    Task<EventMessagesCallbackMessage?> ReadEventMessagesJournalAsync(
        uint listServerAlias,
        DateTime firstTimeStampUtc,
        DateTime secondTimeStampUtc,
        CaseInsensitiveOrderedDictionary<string?> params_);

    AliasResult DefineList(uint listClientAlias, uint listType, CaseInsensitiveOrderedDictionary<string?> listParams);
    
    List<AliasResult> DeleteLists(List<uint> listServerAliases);
    
    Task<List<AliasResult>> AddItemsToListAsync(uint listServerAlias, List<ListItemInfo> itemsToAdd);
    
    Task<List<AliasResult>> RemoveItemsFromListAsync(uint listServerAlias, List<uint> serverAliasesToRemove);

    Task<ElementValuesCallbackMessage?> PollElementValuesChangesAsync(uint listServerAlias);

    Task<List<EventMessagesCallbackMessage>?> PollEventsChangesAsync(uint listServerAlias);
}

public class ContextStatusMessage
{
    /// <summary>
    /// 
    /// </summary>
    public uint StateCode;
}

public class ElementValuesCallbackMessage
{
    #region functions            

    public uint ListClientAlias;

    public readonly List<(uint, ValueStatusTimestamp)> ElementValues = new(1000);        

    #endregion
}

public class EventMessagesCallbackMessage
{
    #region functions

    public uint ListClientAlias;

    public List<EventMessage> EventMessages = new();

    public CaseInsensitiveOrderedDictionary<string?>? CommonFields;
    
    #endregion
}

public class LongrunningPassthroughCallbackMessage : Ssz.Utils.DataAccess.LongrunningPassthroughCallback
{
}

public class AliasResult
{
    public UInt32 StatusCode;
    public string Info = @"";
    public string Label = @"";
    public string Details = @"";
    public UInt32 ClientAlias;
    public UInt32 ServerAlias;        
}

public class EventIdResult
{
    public UInt32 StatusCode;        
    public EventId EventId = null!;
}

public class ListItemInfo
{
    public string ElementId = @"";
    public UInt32 ClientAlias;
}