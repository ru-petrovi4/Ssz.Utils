using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.Extensions.Configuration;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ssz.IdentityServer
{
    public static class IdentityServerConfig
    {
        public static IEnumerable<IdentityResource> GetIdentityResources() =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResource("custom.profile",
                    new[]
                    {
                        JwtClaimTypes.Id,
                        JwtClaimTypes.Name,
                        JwtClaimTypes.GivenName,
                        JwtClaimTypes.MiddleName,
                        JwtClaimTypes.FamilyName,
                        JwtClaimTypes.Role,
                        "pers_number",
                        "office",
                    })
            };

        public static IEnumerable<ApiScope> GetApiScopes() =>
            new ApiScope[]
            {
                new ApiScope("userapi")
            };

        public static IEnumerable<Client> GetClients(IConfiguration configuration)
        {
            int accessTokenLifetimeSeconds = ConfigurationHelper.GetValue<int>(configuration, @"OIDC:AccessTokenLifetimeSeconds", 600);
            int refreshTokenLifetimeSeconds = ConfigurationHelper.GetValue<int>(configuration, @"OIDC:RefreshTokenLifetimeSeconds", 24 * 3600);
            return new Client[]
            {
                new Client
                {
                    ClientId = "userfront",
                    RequireClientSecret = false,
                    ClientSecrets = { new Secret("secret".Sha256()) },
                    AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        "custom.profile",
                        "userapi"
                    },
                    AccessTokenLifetime = accessTokenLifetimeSeconds,
                    AllowOfflineAccess = true, // Allow Refresh Token
                    RefreshTokenUsage = TokenUsage.ReUse,
                    RefreshTokenExpiration = TokenExpiration.Sliding,
                    AbsoluteRefreshTokenLifetime = refreshTokenLifetimeSeconds,
                    SlidingRefreshTokenLifetime = accessTokenLifetimeSeconds * 2,                                        
                }
            };
        }

        public static Dictionary<string, TestUser> GetTestUsers(IConfiguration configuration)
        {
            var testUsers = configuration.GetSection(@"ActiveDirectory:TestUsers").Get<TestUser[]>() ?? new TestUser[0];
            return testUsers.ToDictionary(tu => tu.User, StringComparer.InvariantCultureIgnoreCase);            
        }

        public static IEnumerable<ApiResource> GetApiResources() =>
            new List<ApiResource>();
    }

    public class TestUser
    {
        public string User { get; set; } = @"";

        public string Password { get; set; } = @"";

        public string Name { get; set; } = @"";

        public string Email { get; set; } = @"";

        public string Role { get; set; } = @"";
    }
}


//new Client
//{
//    ClientId = "client",
//    AllowedGrantTypes = GrantTypes.ClientCredentials,
//    ClientSecrets =
//    {
//        new Secret("secret".Sha256())
//    },
//    AllowedScopes = {"userapi"}
//},

//// m2m client credentials flow client
//new Client
//{
//    ClientId = "m2m.client",
//    ClientName = "Client Credentials Client",

//    AllowedGrantTypes = GrantTypes.ClientCredentials,
//    ClientSecrets = { new Secret("511536EF-F270-4058-80CA-1C89C192F69A".Sha256()) },

//    AllowedScopes = { "userapi" }
//},

//// interactive client using code flow + pkce
//new Client
//{
//    ClientId = "interactive",
//    ClientSecrets = { new Secret("49C1A7E1-0C79-4A89-A3D6-A37998FB86B0".Sha256()) },

//    AllowedGrantTypes = GrantTypes.Code,

//    RedirectUris = { "https://localhost:44300/signin-oidc" },
//    FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
//    PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

//    AllowOfflineAccess = true,
//    AllowedScopes = { "openid", "profile", "userapi" }
//},


//new ApiResource("userapi", "Users management API", new[] { JwtClaimTypes.Role })
//{
//    ApiSecrets = new List<Secret>
//    {
//        new Secret("intro".Sha256())
//    }
//}