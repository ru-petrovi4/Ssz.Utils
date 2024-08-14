using IdentityModel;
using IdentityServer4;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using Ssz.IdentityServer.Helpers;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ssz.IdentityServer
{
    public class ADProfileService : IProfileService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestUserResourceOwnerPasswordValidator"/> class.
        /// </summary>
        /// <param name="users">The users.</param>
        /// <param name="clock">The clock.</param>
        public ADProfileService(ILogger<ADProfileService> logger, 
            IConfiguration configuration,             
            IConfigurationProcessor? configurationProcessor = null,
            IUsersAndRolesInfo? usersAndRolesInfo = null)
        {
            _logger = logger;
            _configuration = configuration;            
            _configurationProcessor = configurationProcessor;
            _usersAndRolesInfo = usersAndRolesInfo;
        }

        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            // search by context.Subject.GetSubjectId()

            //context.LogProfileRequest(_logger);

            string userLowerInvariant = context.Subject.GetSubjectId().ToLowerInvariant();
            bool isTestUser = false;
            List<Claim> claims = null!;

            Dictionary<string, string>? allRoles = null;
            if (_usersAndRolesInfo is not null)
                allRoles = (await _usersAndRolesInfo.GetAllRolesAsync()).ToDictionary(r => r, r => r, StringComparer.InvariantCultureIgnoreCase);

            if (_usersAndRolesInfo is not null && _usersAndRolesInfo.TestUsersIsEnabled)
            {
                var testUsers = IdentityServerConfig.GetTestUsers(_configuration);
                if (testUsers.TryGetValue(userLowerInvariant, out var testUser))
                {
                    isTestUser = true;

                    claims = new()
                            {
                                new Claim(JwtClaimTypes.Name, testUser.Name),
                                new Claim(JwtClaimTypes.GivenName, testUser.Name),
                                new Claim(JwtClaimTypes.MiddleName, ""),
                                new Claim(JwtClaimTypes.FamilyName, ""),
                                new Claim(JwtClaimTypes.Email, testUser.Email)
                            };

                    foreach (var role in CsvHelper.ParseCsvLine(@",", testUser.Role))
                    {
                        if (!String.IsNullOrEmpty(role))
                        {
                            string? normalizedRole = null;
                            allRoles?.TryGetValue(role, out normalizedRole);
                            if (!String.IsNullOrEmpty(normalizedRole))
                                claims.Add(new Claim(JwtClaimTypes.Role, normalizedRole));
                        }
                    }
                }
            }            

            if (!isTestUser)
            {
                claims = await LdapHelper.GetClaims(userLowerInvariant, allRoles, _configuration, _configurationProcessor, _logger);
            }

            foreach (var claim in claims.Where(c => c.Type == JwtClaimTypes.Role && 
                String.Equals(c.Value, @"SuperUser", StringComparison.InvariantCultureIgnoreCase))
                .ToArray())
            {
                claims.Remove(claim);
            }
            if (_usersAndRolesInfo is not null && _usersAndRolesInfo.SuperUserIsEnabled)
            {
                string superUser = ConfigurationHelper.GetValue(_configuration, @"ActiveDirectory:SuperUser", @"");
                if (superUser != @"" && String.Equals(userLowerInvariant, superUser, StringComparison.InvariantCultureIgnoreCase))
                {
                    claims!.Add(new Claim(JwtClaimTypes.Role, @"SuperUser"));
                }
            }            

            context.IssuedClaims.AddRange(claims!);

            //context.LogIssuedClaims(_logger);
        }

        public Task IsActiveAsync(IsActiveContext context)
        {            
            context.IsActive = true;            
            return Task.CompletedTask;
        }

        //private Boolean isActive(SearchResult searchResult)
        //{
        //    Attribute userAccountControlAttr = searchResult.getAttributes().get("UserAccountControl");
        //    Integer userAccountControlInt = new Integer((String)userAccoutControlAttr.get());
        //    Boolean disabled = BooleanUtils.toBooleanObject(userAccountControlInt & 0x0002);
        //    return !disabled;
        //}

        #region private fields

        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;        
        private readonly IConfigurationProcessor? _configurationProcessor;
        private readonly IUsersAndRolesInfo? _usersAndRolesInfo;

        #endregion
    }
}


//using System.DirectoryServices;
//using System.DirectoryServices.AccountManagement;
//public class ADProfileService : IProfileService
//{
//    private readonly ILogger _logger;
//    private readonly IConfiguration _configuration;
//    private readonly TimeProvider _clock;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="TestUserResourceOwnerPasswordValidator"/> class.
//    /// </summary>
//    /// <param name="users">The users.</param>
//    /// <param name="clock">The clock.</param>
//    public ADProfileService(ILogger<ADProfileService> logger, IConfiguration configuration, TimeProvider clock)
//    {
//        _logger = logger;
//        _configuration = configuration;
//        _clock = clock;
//    }

//    public Task GetProfileDataAsync(ProfileDataRequestContext context)
//    {
//        string activeDirectoryServerAddress = _configuration.GetValue<string>("ActiveDirectoryServerAddress");
//        using (var adPrincipalContext = new PrincipalContext(ContextType.Domain, activeDirectoryServerAddress))
//        {
//            var user = context.Subject.GetDisplayName();

//            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(adPrincipalContext, IdentityType.SamAccountName, user);

//            var userGroups = userPrincipal.GetGroups();

//            List<Claim> claims = new Claim[]
//            {
//                    new Claim(JwtClaimTypes.Name, userPrincipal.Name),
//                    new Claim(JwtClaimTypes.GivenName, userPrincipal.GivenName),
//                    new Claim(JwtClaimTypes.FamilyName, userPrincipal.DisplayName),
//                    new Claim(JwtClaimTypes.Email, userPrincipal.EmailAddress)
//            }.ToList();

//            foreach (System.DirectoryServices.AccountManagement.Principal principal in userGroups)
//            {
//                // Getting all groups causes JWT to be far too big so just using one as an example.
//                // To see if a user is a "memberOf" a group, use "uPrincipal.IsMemberOf"

//                if (principal.Name == "Domain Users")
//                    claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role", principal.Name));
//            }

//            // To get another AD attribute not in "UserPrincipal" e.g. "Department"
//            string department = "";
//            if (userPrincipal.GetUnderlyingObjectType() == typeof(DirectoryEntry))
//            {
//                // Transition to directory entry to get other properties
//                using (var directoryEntry = (DirectoryEntry)userPrincipal.GetUnderlyingObject())
//                {
//                    var departmetValue = directoryEntry.Properties["department"];
//                    if (departmetValue != null)
//                        department = departmetValue.Value.ToString();
//                }
//            }
//            // Add custom claims in token here based on user properties or any other source
//            claims.Add(new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/department", department));
//            claims.Add(new Claim("upn_custom", userPrincipal.UserPrincipalName));

//            // Filters the claims based on the requested claim types and then adds them to the IssuedClaims collection.
//            //context.AddRequestedClaims(claims);
//            context.IssuedClaims = claims;

//            return Task.CompletedTask;
//        }
//    }

//    public Task IsActiveAsync(IsActiveContext context)
//    {
//        string activeDirectoryServerAddress = _configuration.GetValue<string>("ActiveDirectoryServerAddress");
//        using (var adPrincipalContext = new PrincipalContext(ContextType.Domain, activeDirectoryServerAddress))
//        {
//            var user = context.Subject;

//            Claim userClaim = user.Claims.FirstOrDefault(claimRecord => claimRecord.Type == "sub");

//            UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(adPrincipalContext, IdentityType.SamAccountName, userClaim.Value);

//            // To be active, user must be enabled and not locked out

//            var isLocked = userPrincipal.IsAccountLockedOut();

//            context.IsActive = (bool)(userPrincipal.Enabled & !isLocked);

//            return Task.CompletedTask;
//        }
//    }
//}

//switch (testUser.Role)
//{
//    case TestUser.Observer_Role:
//        {
//            var claims = new[]
//            {
//                                new Claim(JwtClaimTypes.Name, "Тестовый обозреватель"),
//                                new Claim(JwtClaimTypes.GivenName, "Тестовый обозреватель"),
//                                new Claim(JwtClaimTypes.MiddleName, ""),
//                                new Claim(JwtClaimTypes.FamilyName, ""),
//                                new Claim(JwtClaimTypes.Email, "pcobserver2@mail.ru"),
//                                new Claim(JwtClaimTypes.Role, "PazCheck-КОА-Observers")
//                            };

//            context.IssuedClaims.AddRange(claims);

//            return;
//        }
//    case TestUser.Engineer_Role:
//        {
//            var claims = new[]
//    {
//                        new Claim(JwtClaimTypes.Name, "Тестовый инженер"),
//                        new Claim(JwtClaimTypes.GivenName, "Тестовый инженер"),
//                        new Claim(JwtClaimTypes.MiddleName, ""),
//                        new Claim(JwtClaimTypes.FamilyName, ""),
//                        new Claim(JwtClaimTypes.Email, "pceng2@mail.ru"),
//                        new Claim(JwtClaimTypes.Role, "PazCheck-КОА-Engineers")
//                    };

//            context.IssuedClaims.AddRange(claims);

//            return;
//        }
//    case TestUser.Supervisor_Role:
//        {
//            var claims = new[]
//    {
//                        new Claim(JwtClaimTypes.Name, "Тестовый супервайзер"),
//                        new Claim(JwtClaimTypes.GivenName, "Тестовый супервайзер"),
//                        new Claim(JwtClaimTypes.MiddleName, ""),
//                        new Claim(JwtClaimTypes.FamilyName, ""),
//                        new Claim(JwtClaimTypes.Email, "pcsupervisor2@mail.ru"),
//                        new Claim(JwtClaimTypes.Role, "PazCheck-КОА-Supervisors")
//                    };

//            context.IssuedClaims.AddRange(claims);

//            return;
//        }
//    case TestUser.ISAdmin_Role:
//        {
//            var claims = new[]
//    {
//                        new Claim(JwtClaimTypes.Name, "Тестовый администратор ИБ"),
//                        new Claim(JwtClaimTypes.GivenName, "Тестовый администратор ИБ"),
//                        new Claim(JwtClaimTypes.MiddleName, ""),
//                        new Claim(JwtClaimTypes.FamilyName, ""),
//                        new Claim(JwtClaimTypes.Email, "pcisadmin2@mail.ru"),
//                        new Claim(JwtClaimTypes.Role, "PazCheck-КОА-IBadmins")
//                    };

//            context.IssuedClaims.AddRange(claims);

//            return;
//        }
//    case TestUser.Admin_Role:
//        {
//            var claims = new[]
//    {
//                        new Claim(JwtClaimTypes.Name, "Тестовый администратор"),
//                        new Claim(JwtClaimTypes.GivenName, "Тестовый администратор"),
//                        new Claim(JwtClaimTypes.MiddleName, ""),
//                        new Claim(JwtClaimTypes.FamilyName, ""),
//                        new Claim(JwtClaimTypes.Email, "pcadmin2@mail.ru"),
//                        new Claim(JwtClaimTypes.Role, "PazCheck-КОА-Admins")
//                    };

//            context.IssuedClaims.AddRange(claims);

//            return;
//        }
//}