using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using AGC_Management.Entities.Web;
using AGC_Management.Utils;

namespace AGC_Management.Controller
{
    public class UserService
    {
        private static HttpClient client = new HttpClient();

        /// <summary>
        /// Parses the user's discord claim for their `identify` information
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        public async Task<DiscordUserWebClaim> GetInfo(HttpContext httpContext)
        {
            if (!httpContext.User.Identity.IsAuthenticated)
            {
                return null;
            }

            var claims = httpContext.User.Claims;
            bool? verified;
            if (bool.TryParse(claims.FirstOrDefault(x => x.Type == "urn:discord:verified")?.Value, out var _verified))
            {
                verified = _verified;
            }
            else
            {
                verified = null;
            }

            var Role = await AuthUtils.RetrieveRole(ulong.Parse(claims.First(x => x.Type == ClaimTypes.NameIdentifier)
                .Value));
            string? displayname = await AuthUtils.RetrieveRole(ulong.Parse(claims.First(x => x.Type == ClaimTypes.NameIdentifier)
                .Value));
            string? fullQualifiedDiscordName = claims.First(x => x.Type == "FullQualifiedDiscordName").Value;
            var userClaim = new DiscordUserWebClaim()
            {
                UserId = ulong.Parse(claims.First(x => x.Type == ClaimTypes.NameIdentifier).Value),
                Name = claims.First(x => x.Type == ClaimTypes.Name).Value,
                DisplayName = displayname,
                Discriminator = claims.First(x => x.Type == "urn:discord:discriminator").Value,
                Avatar = claims.First(x => x.Type == "urn:discord:avatar").Value,
                WebRole = Role,
                FullQualifiedDiscordName = fullQualifiedDiscordName,
                Email = claims.FirstOrDefault(x => x.Type == ClaimTypes.Email)?.Value,
                Verified = verified
            };

            return userClaim;
        }
    }
}
