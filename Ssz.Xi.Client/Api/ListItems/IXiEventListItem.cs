using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api.ListItems
{
    public interface IXiEventListItem
    {
        Ssz.Utils.DataAccess.EventMessage EventMessage { get; }
    }
}