using Microsoft.Extensions.DependencyInjection;
using SantanderHnApi.Application.Interfaces;
using SantanderHnApi.Application.Services;

namespace SantanderHnApi.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBestStoriesService, BestStoriesService>();
        return services;
    }
}
