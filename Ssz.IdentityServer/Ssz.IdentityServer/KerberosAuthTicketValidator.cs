using Kerberos.NET;
using Kerberos.NET.Crypto;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ssz.IdentityServer
{
    public class KerberosAuthTicketValidator
    {
        public async Task<ClaimsIdentity?> IsValid(string? ticket, string? keytabPath)
        {
            if (!string.IsNullOrEmpty(keytabPath) || !string.IsNullOrEmpty(ticket))
            {
                var kerberosAuth = new KerberosAuthenticator(new KeyTable(File.ReadAllBytes(keytabPath!)));
                var identity = await kerberosAuth.Authenticate(ticket);
                return identity;
            }
            return null;
        }
    }
}
