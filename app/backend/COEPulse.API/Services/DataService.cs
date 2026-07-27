using COEPulse.API.Constants;
using COEPulse.API.DTO;

namespace COEPulse.API.Services;

public record Filters
{
    public int PageNo { get; init; } = 1;
    public int PageSize { get; init; } = 100;
    public int? FromYear { get; init; }
    public int? ToYear { get; init; }
    public VehicleCategory[] Categories { get; init; } = [];
}

public record COEQueryResult(
    IReadOnlyList<COERecord> Records,
    int Total,
    int PageNo,
    int PageSize);

public class DataService(DataSynchronizer synchronizer, IConfiguration config,
    DataFormatter formatter)
{
    private COERecord[]? _records;


    public async Task LoadData()
    {
        await synchronizer.FetchData();
        var csvData = await ReadDataFromFile();
        _records = formatter.FormatData(csvData);
    }

    public COEQueryResult GetRecords(Filters filters)
    {
        if (_records == null)
            throw new InvalidOperationException("Data not loaded. Call LoadData() first.");

        var pageNo = Math.Max(1, filters.PageNo);
        var pageSize = Math.Clamp(filters.PageSize, 1, 2_000);
        IEnumerable<COERecord> query = _records;

        if (filters.FromYear is not null)
            query = query.Where(record => record.Year >= filters.FromYear);
        if (filters.ToYear is not null)
            query = query.Where(record => record.Year <= filters.ToYear);
        if (filters.Categories.Length > 0)
            query = query.Where(record => filters.Categories.Contains(record.VehicleCategory));

        var filtered = query
            .OrderByDescending(record => record.Year)
            .ThenByDescending(record => record.Month)
            .ThenByDescending(record => record.BiddingNumber)
            .ThenBy(record => record.VehicleCategory)
            .ToArray();

        return new COEQueryResult(
            filtered.Skip((pageNo - 1) * pageSize).Take(pageSize).ToArray(),
            filtered.Length,
            pageNo,
            pageSize);
    }


    private async Task<string> ReadDataFromFile()
    {
        var configuredPath = config[Configurations.DATA_SAVE_FILE]
            ?? throw new Exception("Save file path is not configured.");
        var path = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
        return await File.ReadAllTextAsync(path);
    }
}
