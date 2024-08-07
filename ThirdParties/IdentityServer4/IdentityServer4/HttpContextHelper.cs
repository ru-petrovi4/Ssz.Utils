﻿#nullable enable
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;

namespace IdentityServer4
{
    /// <summary>
    /// 
    /// </summary>
    public static class HttpContextHelper
    {
        #region public functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static string GetSourceIpAddress(HttpContext? httpContext)
        {
            if (httpContext is not null)
            {
                var header = httpContext.Request.Headers["X-Forwarded-For"];
                if (header.Count > 0)
                    return header.ToString();
                if (httpContext.Connection.RemoteIpAddress is not null)
                    return httpContext.Connection.RemoteIpAddress.ToString();
            };
            return @"<unknown>";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static string GetSourceHost(HttpContext? httpContext)
        {
            if (httpContext is null)
                return @"<unknown>";

            string sourceIpAddress = GetSourceIpAddress(httpContext);
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(sourceIpAddress);
                if (entry != null)
                {
                    return entry.HostName;
                }
            }
            catch //(SocketException ex)
            {
                //unknown host or
                //not every IP has a name                
            }
            return sourceIpAddress;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static string GetUserLowerInvariant(HttpContext? httpContext)
        {
            if (httpContext is null)
                return @"";
            return (httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? @"").ToLowerInvariant();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public static string[] GetRoles(HttpContext? httpContext)
        {
            if (httpContext is null)
                return new string[0];
            return GetRoles(httpContext.User.Claims);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="claims"></param>
        /// <returns></returns>
        public static string[] GetRoles(IEnumerable<Claim>? claims)
        {
            if (claims is null)
                return new string[0];
            return claims.Where(c => c.Type == "role").Select(c => c.Value).ToArray();
        }

        #endregion
    }
}



