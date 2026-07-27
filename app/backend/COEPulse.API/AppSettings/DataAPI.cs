namespace COEPulse.API.AppSettings;

public class DataAPI
{
    public const string Key = "DataAPI";

    public required string BaseUrl { get; set; }
    public required string InitiateDownload { get; set; }
    public required string PollDownload { get; set; }
    public required string APIKeyHeader { get; set; }
}
