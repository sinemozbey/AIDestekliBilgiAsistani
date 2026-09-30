using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BilgiAsistani.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBilgiAsistani(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<AssistantOptions>(config.GetSection(AssistantOptions.Section));
        services.Configure<LlmServiceOptions>(config.GetSection(LlmServiceOptions.Section));
        services.AddHttpClient(LlmServiceClient.HttpClientName, (sp, http) =>
        {
            var opts = sp.GetRequiredService<IOptions<LlmServiceOptions>>().Value;
            http.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/");
            http.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });
        services.AddSingleton<ILlmClient, LlmServiceClient>();
        // İndeks açılışta bir kez kurulur ve tüm isteklerde paylaşılır.
        services.AddSingleton<QaPipeline>();
        return services;
    }
}
