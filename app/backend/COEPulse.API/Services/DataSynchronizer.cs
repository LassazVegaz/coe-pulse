using COEPulse.API.AppSettings;
using COEPulse.API.Constants;
using COEPulse.API.HttpResponses;
using Microsoft.Extensions.Options;
using System.Net;

namespace COEPulse.API.Services;

public class DataSynchronizer(HttpClient httpClient, IOptions<DataAPI> dataAPI,
    IConfiguration config, ILogger<DataSynchronizer> logger)
{
    public async Task FetchData()
    {
        logger.LogInformation("Initiate downloading data");
        await InitiateDownload();
        logger.LogInformation("Poll downloading data");
        var url = await PollDownload();
        logger.LogInformation("Downloading data");
        var data = await DownloadData(url);
        await SaveDataToFile(data);
        logger.LogInformation("Data saved to the file");
    }


    private async Task InitiateDownload()
    {
        var initiateResponse = await httpClient.GetAsync(dataAPI.Value.InitiateDownload);

        if (initiateResponse.StatusCode != HttpStatusCode.Created)
        {
            throw new Exception(
                $"Failed to initiate download. Status code: {initiateResponse.StatusCode}");
        }
    }

    private async Task<string> PollDownload()
    {
        var pollResponse = await httpClient.GetAsync(dataAPI.Value.PollDownload);
        if (pollResponse.StatusCode != HttpStatusCode.Created)
        {
            throw new Exception(
                $"Failed to poll download. Status code: {pollResponse.StatusCode}");
        }

        // download the data
        var body = await pollResponse.Content.ReadFromJsonAsync<Poll201>();

        if (body?.Data?.Url == null)
            throw new Exception("Download URL is null.");

        return body.Data.Url;
    }

    private async Task<string> DownloadData(string url)
    {
        var downloadResponse = await httpClient.GetAsync(url);
        if (!downloadResponse.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Failed to download data. Status code: {downloadResponse.StatusCode}");
        }
        return await downloadResponse.Content.ReadAsStringAsync();
    }

    private async Task SaveDataToFile(string data)
    {
        var saveFile = config[Configurations.DATA_SAVE_FILE]
            ?? throw new Exception("Save file path is not configured.");
        await File.WriteAllTextAsync(saveFile, data);
    }
}
