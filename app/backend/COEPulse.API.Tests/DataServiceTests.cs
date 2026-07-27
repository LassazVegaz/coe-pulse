using COEPulse.API.AppSettings;
using COEPulse.API.Constants;
using COEPulse.API.DTO;
using COEPulse.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace COEPulse.API.Tests;

public class DataServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"coe-pulse-service-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task GetRecords_FiltersByInclusiveMonthRange()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(DataFile,
            """
            month,bidding_no,vehicle_class,quota,bids_success,bids_received,premium
            2025-12,1,Category A,100,90,120,100000
            2026-01,1,Category A,100,90,120,101000
            2026-02,1,Category A,100,90,120,102000
            """);
        await File.WriteAllTextAsync($"{DataFile}.metadata", "2026-07-22T00:00:00Z");

        var service = CreateDataService();
        await service.LoadData();

        var result = service.GetRecords(new Filters
        {
            FromYear = 2026,
            FromMonth = 1,
            ToYear = 2026,
            ToMonth = 1,
            Categories = [VehicleCategory.A]
        });

        var record = Assert.Single(result.Records);
        Assert.Equal(2026, record.Year);
        Assert.Equal(1, record.Month);
    }

    private string DataFile => Path.Combine(_directory, "data.csv");

    private DataService CreateDataService()
    {
        var settings = Options.Create(new DataAPI
        {
            PrdBaseUrl = "https://production-api.test/",
            OpenBaseUrl = "https://open-api.test/",
            Metadata = "metadata",
            InitiateDownload = "initiate-download",
            PollDownload = "poll-download",
            APIKeyHeader = "x-api-key"
        });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [Configurations.DATA_SAVE_FILE] = DataFile
            })
            .Build();
        var productionClient = new HttpClient(new StubHttpMessageHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"data":{"lastUpdatedAt":"2026-07-22T00:00:00Z"}}""",
                    Encoding.UTF8,
                    "application/json")
            }))
        {
            BaseAddress = new Uri(settings.Value.PrdBaseUrl)
        };
        var openClient = new HttpClient(new StubHttpMessageHandler())
        {
            BaseAddress = new Uri(settings.Value.OpenBaseUrl)
        };
        var synchronizer = new DataSynchronizer(
            settings,
            configuration,
            NullLogger<DataSynchronizer>.Instance,
            productionClient,
            openClient);

        return new DataService(
            synchronizer,
            configuration,
            new DataFormatter(NullLogger<DataFormatter>.Instance),
            NullLogger<DataService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private sealed class StubHttpMessageHandler(params HttpResponseMessage[] responses)
        : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_responses.Count == 0)
                throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
