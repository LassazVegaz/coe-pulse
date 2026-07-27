namespace COEPulse.API.Services;

public class DatasetRefreshService(
    DataService dataService,
    IConfiguration configuration,
    ILogger<DatasetRefreshService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = configuration.GetValue<TimeSpan?>("DataRefreshInterval")
            ?? TimeSpan.FromHours(6);

        if (interval <= TimeSpan.Zero)
            throw new InvalidOperationException("DataRefreshInterval must be greater than zero.");

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await dataService.LoadData(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Periodic dataset refresh failed; continuing to serve the last valid dataset");
            }
        }
    }
}
