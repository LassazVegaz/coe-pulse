namespace COEPulse.API.HttpResponses;

public class Poll201
{
    public int? Code { get; set; }
    public string? ErrorMessage { get; set; }
    public Data? Data { get; set; }
}

public class Data
{
    public string? Status { get; set; }
    public string? Url { get; set; }
}