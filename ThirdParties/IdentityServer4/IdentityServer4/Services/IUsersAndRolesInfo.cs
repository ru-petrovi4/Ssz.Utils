using Microsoft.Extensions.ObjectPool;
using System.Threading.Tasks;

namespace IdentityServer4.Services
{
    /// <summary>
    ///     Gets information about users and roles.
    /// </summary>
    public interface IUsersAndRolesInfo
    {
        /// <summary>
        ///     Gets all user roles names.
        /// </summary>
        /// <returns></returns>
        string[] GetRoles();

        /// <summary>
        ///     Gets all user roles names.
        /// </summary>
        /// <returns></returns>
        Task<string[]> GetRolesAsync();
    }
}
