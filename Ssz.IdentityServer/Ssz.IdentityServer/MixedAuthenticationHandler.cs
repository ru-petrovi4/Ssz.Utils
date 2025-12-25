using Kerberos.NET;
using Kerberos.NET.Crypto;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.IO;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Ssz.IdentityServer;

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

public class MixedAuthenticationDefaults
{
    public const string AuthenticationScheme = "Mixed";
    public const string AuthorizationHeader = "Negotiate";
}

/// <summary>
///     https://habr.com/ru/companies/avanpost/articles/489852/
/// </summary>
public class MixedAuthenticationHandler : CookieAuthenticationHandler
{
    public MixedAuthenticationHandler(IConfiguration configuration, IOptionsMonitor<CookieAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder) : 
        base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authResult = await base.HandleAuthenticateAsync(); // Проверяем, может мы уже //аутентифицированы
        if (!authResult.Succeeded) // Если нет, то пытаемся
        {
            string? authorizationHeader = Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authorizationHeader))
            {
                return AuthenticateResult.Fail("Не получилось");
            }

            // не забываем, что в заголовке приходит не чистый тикет - в начале идет “Negotiate". 
            // Поэтому отрежем лишнее
            var ticket = authorizationHeader.Substring(MixedAuthenticationDefaults.AuthorizationHeader.Length);
            //теперь у нас есть тикет без лишнего мусора
            var kerberosAuthTicketValidator = new KerberosAuthTicketValidator();
            var kerberosIdentity = await kerberosAuthTicketValidator.IsValid(ticket, _configuration["KeytabFileName"]);
            if (kerberosIdentity != null)
            {
                //собираем ClaimsPrincipal
                var principal = new ClaimsPrincipal(kerberosIdentity);
                //создаем тикет аутентификации
                var authTicket = new AuthenticationTicket(principal, MixedAuthenticationDefaults.AuthenticationScheme);

                //если создался, то вызываем базовый метод, чтобы вся кухня хранения аутентификации в cookie сработала
                await base.HandleSignInAsync(principal, authTicket.Properties);
                //возвращаем успешный результат
                return AuthenticateResult.Success(authTicket);
            }
        }
        return authResult;
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401; //статус код “Unauthorized”
        Response.Headers.Append(HeaderNames.WWWAuthenticate, MixedAuthenticationDefaults.AuthorizationHeader);
        return Task.CompletedTask;
    }

    #region private fields

    private readonly IConfiguration _configuration;

    #endregion
}

/// <summary>
///     services.AddAuthentication(MixedAuthenticationDefaults.AuthenticationScheme).AddMixed();
/// </summary>
public static class MixedAuthenticationExtensions
{
    public static AuthenticationBuilder AddMixed(this AuthenticationBuilder builder)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureCookieAuthenticationOptions>());
        return builder.AddScheme<CookieAuthenticationOptions, MixedAuthenticationHandler>(MixedAuthenticationDefaults.AuthenticationScheme, String.Empty, null);
    }
}
