namespace COEPulse.API.AppSettings;

public class DataAPI
{
    public const string Key = "DataAPI";

    public required string PrdBaseUrl { get; set; }
    public required string OpenBaseUrl { get; set; }
    public required string Metadata { get; set; }
    public required string InitiateDownload { get; set; }
    public required string PollDownload { get; set; }
    public required string APIKeyHeader { get; set; }
}
