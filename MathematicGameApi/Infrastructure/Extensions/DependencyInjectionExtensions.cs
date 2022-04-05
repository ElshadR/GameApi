using MathematicGameApi.Infrastructure.Services.Contracts;
using MathematicGameApi.Infrastructure.Services.Implementations;
using Microsoft.Extensions.DependencyInjection;

namespace MathematicGameApi.Infrastructure.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddDependencyInjection(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddTransient<ICoreService, CoreService>();
            return services;
        }
    }
}
