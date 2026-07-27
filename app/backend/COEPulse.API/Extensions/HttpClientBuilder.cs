using COEPulse.API.AppSettings;
using COEPulse.API.Constants;

namespace COEPulse.API.Extensions;

public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddLocalHttpClients(this IServiceCollection services, IConfigurationManager config)
    {
        var dataApiConfig = config.GetSection(DataAPI.Key).Get<DataAPI>()
            ?? throw new Exception($"{DataAPI.Key} is not found in configurations");
        var apiKey = config[Configurations.API_KEY]
            ?? throw new Exception($"{Configurations.API_KEY} is not found in configurations");

        services.AddHttpClient(HttpClientKeys.PRODUCTION, client =>
        {
            client.BaseAddress = new Uri(dataApiConfig.PrdBaseUrl);
            client.DefaultRequestHeaders.Add(dataApiConfig.APIKeyHeader, apiKey);
        }).AddAsKeyed(ServiceLifetime.Singleton);

        return services.AddHttpClient(HttpClientKeys.OPEN, client =>
        {
            client.BaseAddress = new Uri(dataApiConfig.OpenBaseUrl);
            client.DefaultRequestHeaders.Add(dataApiConfig.APIKeyHeader, apiKey);
        }).AddAsKeyed(ServiceLifetime.Singleton);
    }
}
