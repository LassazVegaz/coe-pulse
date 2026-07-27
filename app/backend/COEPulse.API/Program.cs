using COEPulse.API.AppSettings;
using COEPulse.API.Constants;
using COEPulse.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<DataSynchronizer>();

builder.Services.AddHttpClient<DataSynchronizer>(client =>
{
    var config = builder.Configuration.GetValue<DataAPI>(DataAPI.Key)
        ?? throw new Exception($"{DataAPI.Key} is not found in configurations");
    var apiKey = builder.Configuration[Configurations.API_KEY]
        ?? throw new Exception($"{Configurations.API_KEY} is not found in configurations");

    client.BaseAddress = new Uri(config.BaseUrl);
    client.DefaultRequestHeaders.Add(config.APIKeyHeader, apiKey);
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();

app.MapControllers();

var dataService = app.Services.GetRequiredService<DataService>();
await dataService.LoadData();

app.Run();