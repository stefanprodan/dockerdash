using DockerDash.TokenProvider;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace DockerDash
{
    public partial class Startup
    {
        private void ConfigureAuth(IApplicationBuilder app)
        {
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey));

            app.UseTokenProvider(new TokenProviderOptions
            {
                Path = "/token",
                Audience = "Admin",
                Issuer = "DockerDash",
                SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
                IdentityResolver = GetIdentity
            });

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,
                ValidIssuer = "DockerDash",
                ValidateAudience = true,
                ValidAudience = "Admin",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = tokenValidationParameters
            });

        }

        private Task<ClaimsIdentity> GetIdentity(string username, string password)
        {
            if (username.ToLowerInvariant() == this.user && password == this.password)
            {
                return Task.FromResult(new ClaimsIdentity(new GenericIdentity(username, "Token"), new Claim[] { }));
            }

            return Task.FromResult<ClaimsIdentity>(null);
        }
    }
}
