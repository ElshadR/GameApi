using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace MathematicGameApi.Infrastructure.Extensions
{
    public static class JwtAuthenticationServiceExtensions
    {
        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                  .AddJwtBearer(options =>
                  {
                      options.RequireHttpsMetadata = false;
                      options.TokenValidationParameters = new TokenValidationParameters
                      {
                           
                           ValidateIssuer = true,
                           
                           ValidIssuer = configuration["AuthSettings:Insuer"],

                          
                           ValidateAudience = true,
                           
                           ValidAudience = configuration["AuthSettings:Audience"],
                           
                           ValidateLifetime = false,

                          
                           IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["AuthSettings:Key"])),
                           
                           ValidateIssuerSigningKey = true,
                      };
                  });

            return services;
        }
    }
}
