﻿using System.Threading.Tasks;

namespace Novell.Directory.Ldap
{
    public static class ExtensionRegistrations
    {
        static ExtensionRegistrations()
        {
            LdapExtendedResponse.Register(LdapKnownOids.Extensions.WhoAmI, typeof(LdapWhoAmIResponse));
        }

        public static async Task<LdapWhoAmIResponse> WhoAmIAsync(this LdapConnection conn, LdapConstraints cons = null)
        {
            var result = await conn.ExtendedOperationAsync(new LdapWhoAmIOperation(), cons).ConfigureAwait(false);
            if (result is LdapWhoAmIResponse whoami)
            {
                return whoami;
            }

            return new LdapWhoAmIResponse(result.Message);
        }
    }
}
