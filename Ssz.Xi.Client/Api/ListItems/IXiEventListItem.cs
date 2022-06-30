using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api.ListItems
{
    public interface IXiEventListItem
    {
        EventMessage EventMessage { get; }
    }
}