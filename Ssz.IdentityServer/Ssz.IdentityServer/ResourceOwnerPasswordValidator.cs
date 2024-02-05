using IdentityModel;
using IdentityServer4.Validation;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;
using IdentityServer4;
using System.Text.Json;
using Ssz.Utils;
using Ssz.Utils.Logging;
using Microsoft.AspNetCore.Http;
using Ssz.IdentityServer.Helpers;
using Microsoft.Extensions.DependencyInjection;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Linq;
using IdentityServer4.Services;

namespace Ssz.IdentityServer
{
    /// <summary>
    /// Resource owner password validator
    /// </summary>
    /// <seealso cref="IdentityServer4.Validation.IResourceOwnerPasswordValidator" />
    public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="clock"></param>
        /// <param name="informationSecurityEventsLogger"></param>
        /// <param name="serviceProvider"></param>
        public ResourceOwnerPasswordValidator(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ResourceOwnerPasswordValidator> logger, 
            IConfiguration configuration,             
            IInformationSecurityEventsLogger informationSecurityEventsLogger, 
            IServiceProvider serviceProvider,
            IConfigurationProcessor? configurationProcessor = null,
            IUsersAndRolesInfo? usersAndRolesInfo = null)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _configuration = configuration;            
            _informationSecurityEventsLogger = informationSecurityEventsLogger;
            _serviceProvider = serviceProvider;
            _configurationProcessor = configurationProcessor;
            _usersAndRolesInfo = usersAndRolesInfo;
        }

        /// <summary>
        /// Validates the resource owner password credential
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            string user = context.UserName.ToLowerInvariant();
            var httpContext = _httpContextAccessor.HttpContext;

            string errorReason = @"";
            try
            {
                if (httpContext is not null && (String.IsNullOrEmpty(user) || String.Equals(user, @"current_user", StringComparison.InvariantCultureIgnoreCase)))
                {
                    List<string?> apiProxyIdHeaders = httpContext!.Request.Headers[@"ApiProxyId"].ToList();
                    string apiProxyId = ConfigurationHelper.GetValue<string>(_configuration, @"ApiProxyId", @"");
                    if (apiProxyId != @"" && apiProxyIdHeaders.Count == 1 && apiProxyId == apiProxyIdHeaders[0])
                    {
                        var basicAuthenticationSecretParser = ActivatorUtilities.CreateInstance<BasicAuthenticationSecretParser>(_serviceProvider);
                        var parsedSecret = await basicAuthenticationSecretParser.ParseAsync(httpContext);
                        if (parsedSecret is not null && parsedSecret.Type == IdentityServerConstants.ParsedSecretTypes.SharedSecret)
                        {
                            _logger.LogDebug("Parser found secret: {type}", basicAuthenticationSecretParser.GetType().Name);

                            user = parsedSecret.Id.ToLowerInvariant();

                            if (!String.IsNullOrEmpty(user))
                                context.Result = new GrantValidationResult(
                                    user,
                                    OidcConstants.AuthenticationMethods.Password, DateTime.UtcNow);
                        }
                    }

                    return;
                }

                if (_usersAndRolesInfo is not null && _usersAndRolesInfo.TestUsersIsEnabled)
                {
                    var testUsers = IdentityServerConfig.GetTestUsers(_configuration);
                    if (testUsers.TryGetValue(user, out var testUser))
                    {
                        if (context.Password == testUser.Password)
                        {
                            context.Result = new GrantValidationResult(
                                user,
                                OidcConstants.AuthenticationMethods.Password, DateTime.UtcNow);
                        }

                        return;
                    }                    
                }

                String loginDN = await LdapHelper.GetDnForUser(user, _configuration, _configurationProcessor, _logger);
                if (loginDN == @"")
                {
                    _logger.LogError("Cannot find User in AD: " + user);
                    return;
                }
                String password = context.Password;

                using (var ldapConnection = LdapHelper.CreateLdapConnection(_configuration, _logger))
                {
                    String ldapHost = ConfigurationHelper.GetValue<string>(_configuration, @"ActiveDirectory:Server", @"");
                    if (_configurationProcessor is not null)
                        ldapHost = _configurationProcessor.ProcessValue(ldapHost);

                    int ldapPort = ConfigurationHelper.GetValue<int>(_configuration, @"ActiveDirectory:LdapPort", LdapConnection.DefaultPort);
                    int ldapVersion = ConfigurationHelper.GetValue<int>(_configuration, @"ActiveDirectory:LdapVersion", LdapConnection.LdapV3);                                       

                    // connect to the server
                    await ldapConnection.ConnectAsync(ldapHost, ldapPort);                    

                    // authenticate to the server
                    await ldapConnection.BindAsync(ldapVersion, loginDN, password);

                    context.Result = new GrantValidationResult(
                        user,
                        OidcConstants.AuthenticationMethods.Password, DateTime.UtcNow);
                }
            }
            catch (LdapException ex)
            {
                if (ex.ResultCode == LdapException.InvalidCredentials)
                {
                    errorReason = Properties.Resources.Error_InvalidCredentials;
                    _logger.LogError("Invalid Credentials, User: " + user);
                }
                else if (ex.ResultCode == LdapException.NoSuchObject)
                {
                    errorReason = Properties.Resources.Error_NoSuchObject;
                    _logger.LogError("No such entry, User: " + user);
                }
                else if (ex.ResultCode == LdapException.NoSuchAttribute)
                {
                    errorReason = Properties.Resources.Error_NoSuchAttribute;
                    _logger.LogError("No such attribute, User: " + user);
                }
                else
                {
                    errorReason = Properties.Resources.Error_LdapIOException;
                    _logger.LogError(ex, @"LdapException, User: " + user);
                }
            }
            catch (System.IO.IOException ex)
            {
                errorReason = Properties.Resources.Error_LdapIOException;
                _logger.LogError(ex, @"IOException, User: " + user);
            }
            catch (Exception ex)
            {
                errorReason = Properties.Resources.Error_LdapIOException;
                _logger.LogError(ex, @"Exception, User: " + user);
            }
            finally
            {
                bool isSuperUser = false;

                if (_usersAndRolesInfo is not null && _usersAndRolesInfo.SuperUserIsEnabled)
                {
                    string superUser = ConfigurationHelper.GetValue(_configuration, @"ActiveDirectory:SuperUser", @"");
                    if (superUser != @"" && String.Equals(user, superUser, StringComparison.InvariantCultureIgnoreCase))
                        isSuperUser = true;
                }

                if (isSuperUser)
                {
                    if (!context.Result.IsError)
                    {
                        _informationSecurityEventsLogger.InformationSecurityEvent(user,
                            HttpContextHelper.GetSourceIpAddress(httpContext),
                            HttpContextHelper.GetSourceHost(httpContext),                            
                            0x01,
                            9,
                            true,
                            Properties.Resources.SuperUserLogIn_Event,
                            user,
                            Properties.Resources.ObjectSystem,
                            @"",
                            Properties.Resources.SuperUserLogIn_Event);
                    }
                    else
                    {
                        _informationSecurityEventsLogger.InformationSecurityEvent(user,
                            HttpContextHelper.GetSourceIpAddress(httpContext),
                            HttpContextHelper.GetSourceHost(httpContext),                            
                            0x01,
                            9,
                            false,
                            Properties.Resources.SuperUserLogIn_Event,
                            user,
                            Properties.Resources.ObjectSystem,
                            @"",
                            errorReason);
                    }
                }
                else
                {
                    if (!context.Result.IsError)
                    {
                        _informationSecurityEventsLogger.InformationSecurityEvent(user,
                            HttpContextHelper.GetSourceIpAddress(httpContext),
                            HttpContextHelper.GetSourceHost(httpContext),                            
                            0x01,
                            3,
                            true,
                            Properties.Resources.UserLogIn_Event,
                            user,
                            Properties.Resources.ObjectSystem,
                            @"",
                            Properties.Resources.UserLogIn_Event);
                    }
                    else
                    {
                        _informationSecurityEventsLogger.InformationSecurityEvent(user,
                            HttpContextHelper.GetSourceIpAddress(httpContext),
                            HttpContextHelper.GetSourceHost(httpContext),                            
                            0x01,
                            8,
                            false,
                            Properties.Resources.UserLogIn_Event,
                            user,
                            Properties.Resources.ObjectSystem,
                            @"",
                            errorReason);
                    }
                }                
            }
        }

        #region private fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;        
        private readonly IInformationSecurityEventsLogger _informationSecurityEventsLogger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfigurationProcessor? _configurationProcessor;
        private readonly IUsersAndRolesInfo? _usersAndRolesInfo;

        #endregion
    }
}


//using System.DirectoryServices.AccountManagement;

//public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
//{
//    private readonly ILogger _logger;
//    private readonly IConfiguration _configuration;
//    private readonly ISystemClock _clock;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="TestUserResourceOwnerPasswordValidator"/> class.
//    /// </summary>
//    /// <param name="users">The users.</param>
//    /// <param name="clock">The clock.</param>
//    public ResourceOwnerPasswordValidator(ILogger<ResourceOwnerPasswordValidator> logger, IConfiguration configuration, ISystemClock clock)
//    {
//        _logger = logger;
//        _configuration = configuration;
//        _clock = clock;
//    }

//    /// <summary>
//    /// Validates the resource owner password credential
//    /// </summary>
//    /// <param name="context">The context.</param>
//    /// <returns></returns>
//    public Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
//    {
//        string activeDirectoryServerAddress = _configuration.GetValue<string>("ActiveDirectoryServerAddress");
//        using (var adPrincipalContext = new PrincipalContext(ContextType.Domain, activeDirectoryServerAddress, "rodc", "1"))
//        {
//            if (adPrincipalContext.ValidateCredentials(context.UserName, context.Password))
//            {
//                // create a new unique subject id
//                var subjectId = CryptoRandom.CreateUniqueId(format: CryptoRandom.OutputFormat.Hex);
//                var claims = new List<Claim>();
//                try
//                {
//                    var userPrincipal = UserPrincipal.FindByIdentity(adPrincipalContext, IdentityType.SamAccountName, context.UserName);
//                    var first = userPrincipal.GivenName;
//                    var last = userPrincipal.Surname;
//                    if (!String.IsNullOrEmpty(first) && !String.IsNullOrEmpty(last))
//                    {
//                        claims.Add(new Claim(JwtClaimTypes.Name, first + " " + last));
//                    }
//                    else if (!String.IsNullOrEmpty(first))
//                    {
//                        claims.Add(new Claim(JwtClaimTypes.Name, first));
//                    }
//                    else if (!String.IsNullOrEmpty(last))
//                    {
//                        claims.Add(new Claim(JwtClaimTypes.Name, last));
//                    }
//                }
//                catch
//                {
//                    _logger.LogError("adPrincipalContext.ValidateCredentials(context.UserName, context.Password) Error");
//                    claims.Add(new Claim(JwtClaimTypes.Name, context.UserName));
//                }

//                context.Result = new GrantValidationResult(
//                    subjectId,
//                    OidcConstants.AuthenticationMethods.Password, _clock.UtcNow.UtcDateTime,
//                    claims);
//            }
//        }

//        return Task.CompletedTask;
//    }
//}
