using COEPulse.API.AppSettings;
using COEPulse.API.Extensions;
using COEPulse.API.Services;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<DataAPI>().BindConfiguration(DataAPI.Key);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<DataSynchronizer>();
builder.Services.AddSingleton<DataFormatter>();
builder.Services.AddHostedService<DatasetRefreshService>();

builder.Services.AddLocalHttpClients(builder.Configuration);


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthorization();
app.UseCors();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

var dataService = app.Services.GetRequiredService<DataService>();
await dataService.LoadData();

app.Run();
