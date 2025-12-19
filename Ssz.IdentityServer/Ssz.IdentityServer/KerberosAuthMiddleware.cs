using Kerberos.NET;
using Kerberos.NET.Client;
using Kerberos.NET.Credentials;
using Kerberos.NET.Crypto;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using StatusCodes = Microsoft.AspNetCore.Http.StatusCodes;

namespace Ssz.IdentityServer.Helpers.GSSAPI;

/// <summary>
/// Middleware для аутентификации через Kerberos.NET
/// Обрабатывает GSS-API токены от браузера
/// </summary>
public class KerberosAuthMiddleware
{
    public KerberosAuthMiddleware(
        RequestDelegate nextMiddleware, 
        IConfiguration configuration,
        ILogger<KerberosAuthMiddleware> logger)
    {
        _nextMiddleware = nextMiddleware;
        _configuration = configuration;
        _logger = logger;

        string keytabPath = ConfigurationHelper.GetValue(_configuration, IdentityServerConstants.ConfigurationKey_Kerberos_KeytabPath, @""); // "/etc/krb5.keytab";
        if (!String.IsNullOrEmpty(keytabPath))
        {
            try
            {
                var keyTable = new KeyTable(System.IO.File.ReadAllBytes(keytabPath));
                _authenticator = new KerberosAuthenticator(keyTable);                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot read keytab file.");
            }
        }        
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == @"/connect/token" &&
                context.Request.Form.ContainsKey(@"username") &&
                String.IsNullOrEmpty(context.Request.Form[@"username"]))
        {
            try
            {
                string userName = @"";

                if (_authenticator is not null)
                {
                    // Проверяем Authorization: Negotiate заголовок
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

                    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Negotiate "))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.Headers["WWW-Authenticate"] = "Negotiate";
                        return;
                    }

                    var tokenBase64 = authHeader.Substring("Negotiate ".Length).Trim();

                    ClaimsIdentity identity;
                    try
                    {
                        identity = await _authenticator.Authenticate(tokenBase64);
                        userName = identity?.Name ?? @""; // обычно user@G-NEFT.LOCAL
                        _logger.LogInformation("KerberosAuthenticator.Authenticate succeeded. Raw UserName: " + userName);
                        int i = userName.IndexOf("@");
                        if (i > 0)
                            userName = userName.Substring(0, i);                        
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "KerberosAuthenticator.Authenticate error.");
                    }
                }

                // Кладём проверенное имя в заголовок ответа
                context.Response.Headers[IdentityServerConstants.Header_Authorization_Kerberos_GSSAPI_SPNEGO] = userName;                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Необработанное исключение в KerberosAuthMiddleware");
                throw;
            }            
        }

        await _nextMiddleware(context);
    }

    #region private fields

    private readonly RequestDelegate _nextMiddleware;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KerberosAuthMiddleware> _logger;
    private readonly KerberosAuthenticator? _authenticator;

    #endregion
}


