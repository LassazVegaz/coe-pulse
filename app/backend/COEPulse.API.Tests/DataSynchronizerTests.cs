using COEPulse.API.AppSettings;
using COEPulse.API.Constants;
using COEPulse.API.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace COEPulse.API.Tests;

public class DataSynchronizerTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"coe-pulse-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task FetchData_DownloadsAndStoresDataset_WhenNoLocalCopyExists()
    {
        var handler = new StubHttpMessageHandler(
            Json(HttpStatusCode.OK, """{"data":{"lastUpdatedAt":"2026-07-22T00:00:00Z"}}"""),
            Json(HttpStatusCode.Created, "{}"),
            Json(HttpStatusCode.Created, """{"data":{"url":"https://download.test/coe.csv"}}"""),
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("year,month\n2026,7", Encoding.UTF8, "text/csv")
            });
        var synchronizer = CreateSynchronizer(handler);

        var downloaded = await synchronizer.FetchData();

        Assert.True(downloaded);
        Assert.Equal("year,month\n2026,7", await File.ReadAllTextAsync(DataFile));
        Assert.Equal("2026-07-22T00:00:00Z",
            await File.ReadAllTextAsync($"{DataFile}.metadata"));
        Assert.Equal(4, handler.RequestCount);
    }

    [Fact]
    public async Task FetchData_SkipsDownload_WhenDatasetTimestampHasNotChanged()
    {
        Directory.CreateDirectory(_directory);
        await File.WriteAllTextAsync(DataFile, "cached csv");
        await File.WriteAllTextAsync($"{DataFile}.metadata", "2026-07-22T00:00:00Z");
        var handler = new StubHttpMessageHandler(
            Json(HttpStatusCode.OK, """{"data":{"lastUpdatedAt":"2026-07-22T00:00:00Z"}}"""));
        var synchronizer = CreateSynchronizer(handler);

        var downloaded = await synchronizer.FetchData();

        Assert.False(downloaded);
        Assert.Equal("cached csv", await File.ReadAllTextAsync(DataFile));
        Assert.Equal(1, handler.RequestCount);
    }

    private string DataFile => Path.Combine(_directory, "data.csv");

    private DataSynchronizer CreateSynchronizer(HttpMessageHandler handler)
    {
        var settings = Options.Create(new DataAPI
        {
            BaseUrl = "https://api.test/",
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
        var client = new HttpClient(handler) { BaseAddress = new Uri(settings.Value.BaseUrl) };

        return new DataSynchronizer(
            client, settings, configuration, NullLogger<DataSynchronizer>.Instance);
    }

    private static HttpResponseMessage Json(HttpStatusCode statusCode, string body) =>
        new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

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
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            if (_responses.Count == 0)
                throw new InvalidOperationException($"Unexpected request: {request.RequestUri}");

            return Task.FromResult(_responses.Dequeue());
        }
    }
}
