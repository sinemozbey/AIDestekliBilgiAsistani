using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BilgiAsistani.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBilgiAsistani(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AssistantOptions>(config.GetSection(AssistantOptions.Section));
        // İndeks açılışta bir kez kurulur ve tüm isteklerde paylaşılır.
        services.AddSingleton<QaPipeline>();
        return services;
    }
}
