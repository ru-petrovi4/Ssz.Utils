using IdentityModel;
using IdentityServer4.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ssz.IdentityServer.Helpers
{
    public static class LdapHelper
    {
        #region public functions

        public static LdapConnection CreateLdapConnection(IConfiguration configuration, ILogger logger)
        {
            LdapConnectionOptions ldapConnectionOptions = new();            
            if (ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:UseSsl", false))
                ldapConnectionOptions.UseSsl();
            if (ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:CheckCertificateRevocation", false))
                ldapConnectionOptions.CheckCertificateRevocation();
            return new LdapConnection(ldapConnectionOptions);
        }

        public static async Task<LdapConnection?> CreatePreparedLdapConnectionAsync(IConfiguration configuration, IConfigurationProcessor? configurationProcessor, ILogger logger)
        {
            string ldapHost = ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:Server", @"");            
            int ldapPort = ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:LdapPort", LdapConnection.DefaultPort);
            int ldapVersion = ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:LdapVersion", LdapConnection.LdapV3);
            string activeDirectory_ServiceAccount_DN = ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:ServiceAccount:DN", @"");
            string activeDirectory_ServiceAccount_Password = ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:ServiceAccount:Password", @"");
            if (configurationProcessor is not null)
            {
                ldapHost = configurationProcessor.ProcessValue(ldapHost);                
                activeDirectory_ServiceAccount_DN = configurationProcessor.ProcessValue(activeDirectory_ServiceAccount_DN);
                activeDirectory_ServiceAccount_Password = configurationProcessor.ProcessValue(activeDirectory_ServiceAccount_Password);
            }

            var ldapConnection = CreateLdapConnection(configuration, logger);
            
            try
            {
                // connect to the server
                await ldapConnection.ConnectAsync(ldapHost, ldapPort);

                // authenticate to the server
                await ldapConnection.BindAsync(ldapVersion, activeDirectory_ServiceAccount_DN, activeDirectory_ServiceAccount_Password);

                return ldapConnection;
            }
            catch (LdapException ex)
            {
                if (ex.ResultCode == LdapException.InvalidCredentials)
                {
                    logger.LogError("Invalid Credentials, ServiceAccount: " + activeDirectory_ServiceAccount_DN);
                }
                else if (ex.ResultCode == LdapException.NoSuchObject)
                {
                    logger.LogError("No such entry, ServiceAccount: " + activeDirectory_ServiceAccount_DN);
                }
                else if (ex.ResultCode == LdapException.NoSuchAttribute)
                {
                    logger.LogError("No such attribute, ServiceAccount: " + activeDirectory_ServiceAccount_DN);
                }
                else
                {
                    logger.LogError(ex, @"LdapException, ServiceAccount: " + activeDirectory_ServiceAccount_DN);
                }
            }
            catch (System.IO.IOException ex)
            {
                logger.LogError(ex, @"IOException, ServiceAccount: " + activeDirectory_ServiceAccount_DN);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, @"Exception, ServiceAccount: " + activeDirectory_ServiceAccount_DN);
            }            

            return null;
        }

        public static async Task<List<Claim>> GetClaims(string user, IConfiguration configuration, IConfigurationProcessor? configurationProcessor, ILogger logger)
        {            
            string activeDirectory_UsersDN = ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:UsersDN", @"");
            if (configurationProcessor is not null)
                activeDirectory_UsersDN = configurationProcessor.ProcessValue(activeDirectory_UsersDN);

            using var preparedLdapConnection = await CreatePreparedLdapConnectionAsync(configuration, configurationProcessor, logger);
            if (preparedLdapConnection is null)
                return new List<Claim>();

            var groups = await GetGroupsForUser(preparedLdapConnection, user, configuration);

            // create a new unique subject id
            string subjectId = user;
            string name;
            string first = @"";
            string last = @"";

            LdapSearchQueue ldapSearchQueue = await preparedLdapConnection.SearchAsync(
                activeDirectory_UsersDN,
                LdapConnection.ScopeSub,
                $"(sAMAccountName={user})",
                new string[] { "cn", "givenName", "sn" },
                false,
                null as LdapSearchQueue);

            LdapMessage ldapMessage;
            while ((ldapMessage = ldapSearchQueue.GetResponse()) != null)
            {
                if (ldapMessage is LdapSearchResult ldapSearchResult)
                {
                    LdapEntry ldapEntry = ldapSearchResult.Entry;

                    LdapAttribute? ldapAttribute;
                    ldapEntry.GetAttributeSet().TryGetValue("givenName", out ldapAttribute);
                    if (ldapAttribute != null)
                    {
                        foreach (string value in ldapAttribute.StringValueArray)
                        {
                            first = value;
                        }
                    }

                    ldapEntry.GetAttributeSet().TryGetValue("sn", out ldapAttribute);
                    if (ldapAttribute != null)
                    {
                        foreach (string value in ldapAttribute.StringValueArray)
                        {
                            last = value;
                        }
                    }

                    //ldapEntry.GetAttributeSet().TryGetValue("userAccountControl", out ldapAttribute);
                    //if (ldapAttribute != null)
                    //{
                    //    foreach (string value in ldapAttribute.StringValueArray)
                    //    {
                    //        int userAccountControlInt = new Any(value).ValueAsInt32(false);
                    //        isDisabled = (userAccountControlInt & 0x0002) != 0;
                    //    }
                    //}
                }
            }

            if (!string.IsNullOrEmpty(first) && !string.IsNullOrEmpty(last))
            {
                name = first + " " + last;
            }
            else if (!string.IsNullOrEmpty(first))
            {
                name = first;
            }
            else if (!string.IsNullOrEmpty(last))
            {
                name = last;
            }
            else
            {
                name = user;
            }

            List<Claim> claims = new()
                    {
                        new Claim(JwtClaimTypes.Name, name),
                        new Claim(JwtClaimTypes.GivenName, first),
                        new Claim(JwtClaimTypes.FamilyName, last),
                        new Claim(JwtClaimTypes.Email, ""),
                        //new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean)
                    };
            foreach (string group in groups)
            {
                claims.Add(new Claim(JwtClaimTypes.Role, group));
            }

            return claims;            
        }

        public static async Task<HashSet<string>> GetGroupsForUser(LdapConnection preparedLdapConnection, string user, IConfiguration configuration)
        {
            var groups = new HashSet<string>();

            string activeDirectory_UsersDN = ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:UsersDN", @"");
            foreach (string group in await GetGroups(preparedLdapConnection, activeDirectory_UsersDN, user))
            {
                groups.Add(group);

                foreach (string parentGroup in await GetGroups(preparedLdapConnection, activeDirectory_UsersDN, group))
                    groups.Add(parentGroup);
            }

            return groups;
        }

        public static async Task<string> GetDnForUser(string user, IConfiguration configuration, IConfigurationProcessor? configurationProcessor, ILogger logger)
        {            
            string activeDirectory_UsersDN = ConfigurationHelper.GetValue(configuration, @"ActiveDirectory:UsersDN", @"");
            if (configurationProcessor is not null)
                activeDirectory_UsersDN = configurationProcessor.ProcessValue(activeDirectory_UsersDN);

            using var preparedLdapConnection = await CreatePreparedLdapConnectionAsync(configuration, configurationProcessor, logger);
            if (preparedLdapConnection is null)
                return @"";

            try
            {
                LdapSearchQueue ldapSearchQueue = await preparedLdapConnection.SearchAsync(
                    activeDirectory_UsersDN,
                    LdapConnection.ScopeSub,
                    $"(sAMAccountName={user})",
                    new string[] { "distinguishedName" },
                    false,
                    null as LdapSearchQueue);

                LdapMessage ldapMessage;
                while ((ldapMessage = ldapSearchQueue.GetResponse()) != null)
                {
                    if (ldapMessage is LdapSearchResult ldapSearchResult)
                    {
                        LdapEntry ldapEntry = ldapSearchResult.Entry;
                        if (ldapEntry.GetAttributeSet().TryGetValue("distinguishedName", out LdapAttribute? ldapAttribute))
                            return ldapAttribute.StringValue;
                    }
                }
            }                
            catch (Exception ex)
            {
                logger.LogError(ex, @"Exception");
            }            

            return @"";            
        }


        #endregion

        #region private functions

        private static async Task<IEnumerable<string>> GetGroups(LdapConnection preparedLdapConnection, string baseDN, string user)
        {
            var result = new List<string>();

            try
            {
                LdapSearchQueue ldapSearchQueue = await preparedLdapConnection.SearchAsync(
                    baseDN,
                    LdapConnection.ScopeSub,
                    $"(sAMAccountName={user})",
                    new string[] { "cn", "memberOf" },
                    false,
                    null as LdapSearchQueue);

                LdapMessage ldapMessage;
                while ((ldapMessage = ldapSearchQueue.GetResponse()) != null)
                {
                    if (ldapMessage is LdapSearchResult ldapSearchResult)
                    {
                        LdapEntry ldapEntry = ldapSearchResult.Entry;
                        foreach (string value in GetGroups(ldapEntry))
                            result.Add(value);
                    }
                    else
                        continue;
                }
            }
            catch
            {
            }            

            return result;
        }

        private static IEnumerable<string> GetGroups(LdapEntry ldapEntry)
        {
            ldapEntry.GetAttributeSet().TryGetValue("memberOf", out LdapAttribute? ldapAttribute);

            if (ldapAttribute == null) yield break;

            foreach (string value in ldapAttribute.StringValueArray)
            {
                string? group = GetGroup(value);
                if (!string.IsNullOrEmpty(group))
                    yield return group;
            }
        }

        private static string? GetGroup(string value)
        {
            Match match = Regex.Match(value, "^CN=([^,]*)");

            if (!match.Success) return null;

            return match.Groups[1].Value;
        }

        #endregion
    }
}
