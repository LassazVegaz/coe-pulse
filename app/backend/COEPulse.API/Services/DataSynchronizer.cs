using COEPulse.API.AppSettings;
using COEPulse.API.Constants;
using COEPulse.API.HttpResponses;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace COEPulse.API.Services;

public class DataSynchronizer(HttpClient httpClient, IOptions<DataAPI> dataAPI,
    IConfiguration config, ILogger<DataSynchronizer> logger)
{
    public async Task<bool> FetchData(CancellationToken cancellationToken = default)
    {
        var dataFile = GetDataFilePath();
        var metadataFile = $"{dataFile}.metadata";
        var remoteLastUpdated = await GetLastUpdatedAt(cancellationToken);

        if (File.Exists(dataFile) && File.Exists(metadataFile))
        {
            var localLastUpdated = await File.ReadAllTextAsync(metadataFile, cancellationToken);
            if (string.Equals(localLastUpdated, remoteLastUpdated, StringComparison.Ordinal))
            {
                logger.LogInformation("Dataset has not changed since {LastUpdated}; using the local copy",
                    remoteLastUpdated);
                return false;
            }
        }

        logger.LogInformation("Initiate downloading data");
        await InitiateDownload(cancellationToken);
        logger.LogInformation("Poll downloading data");
        var url = await PollDownload(cancellationToken);
        logger.LogInformation("Downloading data");
        var data = await DownloadData(url, cancellationToken);
        await SaveDataToFile(data, cancellationToken);
        await File.WriteAllTextAsync(metadataFile, remoteLastUpdated, cancellationToken);
        logger.LogInformation("Data saved to the file");
        return true;
    }

    private async Task<string> GetLastUpdatedAt(CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(dataAPI.Value.Metadata, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            await LogErrorResponse("Failed to read dataset metadata", response, cancellationToken);
            throw new HttpRequestException(
                $"Failed to read dataset metadata. Status code: {response.StatusCode}");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return FindStringProperty(document.RootElement, "lastUpdatedAt")
            ?? throw new InvalidDataException("Metadata response does not contain lastUpdatedAt.");
    }

    private static string? FindStringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) && property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();

                var nestedValue = FindStringProperty(property.Value, propertyName);
                if (nestedValue is not null)
                    return nestedValue;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nestedValue = FindStringProperty(item, propertyName);
                if (nestedValue is not null)
                    return nestedValue;
            }
        }

        return null;
    }

    private async Task InitiateDownload(CancellationToken cancellationToken)
    {
        using var initiateResponse =
            await httpClient.GetAsync(dataAPI.Value.InitiateDownload, cancellationToken);

        if (initiateResponse.StatusCode != HttpStatusCode.Created)
        {
            await LogErrorResponse("Failed to initiate download", initiateResponse, cancellationToken);
            throw new HttpRequestException(
                $"Failed to initiate download. Status code: {initiateResponse.StatusCode}");
        }
    }

    private async Task<string> PollDownload(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= 10; attempt++)
        {
            using var pollResponse =
                await httpClient.GetAsync(dataAPI.Value.PollDownload, cancellationToken);
            if (pollResponse.StatusCode == HttpStatusCode.Created)
            {
                var body = await pollResponse.Content.ReadFromJsonAsync<Poll201>(
                    cancellationToken: cancellationToken);

                if (body?.Data?.Url is not null)
                    return body.Data.Url;
            }
            else if (pollResponse.StatusCode is not HttpStatusCode.Accepted)
            {
                await LogErrorResponse("Failed to poll download", pollResponse, cancellationToken);
                throw new HttpRequestException(
                    $"Failed to poll download. Status code: {pollResponse.StatusCode}");
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException("Dataset download was not ready after 10 polling attempts.");
    }

    private async Task<string> DownloadData(string url, CancellationToken cancellationToken)
    {
        using var downloadResponse = await httpClient.GetAsync(url, cancellationToken);
        if (!downloadResponse.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Failed to download data. Status code: {downloadResponse.StatusCode}");
        }
        return await downloadResponse.Content.ReadAsStringAsync(cancellationToken);
    }

    private async Task SaveDataToFile(string data, CancellationToken cancellationToken)
    {
        var saveFile = GetDataFilePath();
        Directory.CreateDirectory(Path.GetDirectoryName(saveFile)!);

        var temporaryFile = $"{saveFile}.tmp";
        await File.WriteAllTextAsync(temporaryFile, data, cancellationToken);
        File.Move(temporaryFile, saveFile, overwrite: true);
    }

    private string GetDataFilePath()
    {
        var configuredPath = config[Configurations.DATA_SAVE_FILE]
            ?? throw new InvalidOperationException("Save file path is not configured.");
        return Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
    }

    private async Task LogErrorResponse(string message, HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        logger.LogError("{message}\nCode:{code}\nContent:{content}",
            message, response.StatusCode, content);
    }
}
