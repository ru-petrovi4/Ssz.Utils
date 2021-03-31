using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api.ListItems
{
    public interface IXiEventListItem
    {
        Ssz.Utils.DataSource.EventMessage EventMessage { get; }
    }
}