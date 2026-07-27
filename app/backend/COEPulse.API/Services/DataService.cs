using COEPulse.API.Constants;
using COEPulse.API.DTO;

namespace COEPulse.API.Services;

public record Filters
{
    public int PageNo { get; init; } = 1;
    public int PageSize { get; init; } = 100;
    public int? FromYear { get; init; }
    public int? FromMonth { get; init; }
    public int? ToYear { get; init; }
    public int? ToMonth { get; init; }
    public VehicleCategory[] Categories { get; init; } = [];
}

public record COEQueryResult(
    IReadOnlyList<COERecord> Records,
    int Total,
    int PageNo,
    int PageSize);

public class DataService(DataSynchronizer synchronizer, IConfiguration config,
    DataFormatter formatter, ILogger<DataService> logger)
{
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private COERecord[]? _records;


    public async Task LoadData(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var downloaded = await synchronizer.FetchData(cancellationToken);
            if (!downloaded && Volatile.Read(ref _records) is not null)
                return;

            var csvData = await ReadDataFromFile(cancellationToken);
            var refreshedRecords = formatter.FormatData(csvData);
            Volatile.Write(ref _records, refreshedRecords);
            logger.LogInformation("Loaded {RecordCount} COE records into memory",
                refreshedRecords.Length);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public COEQueryResult GetRecords(Filters filters)
    {
        var records = Volatile.Read(ref _records);
        if (records is null)
            throw new InvalidOperationException("Data not loaded. Call LoadData() first.");

        var pageNo = Math.Max(1, filters.PageNo);
        var pageSize = Math.Clamp(filters.PageSize, 1, 2_000);
        IEnumerable<COERecord> query = records;

        if (filters.FromYear is not null)
        {
            var from = filters.FromYear.Value * 100 + Math.Clamp(filters.FromMonth ?? 1, 1, 12);
            query = query.Where(record => record.Year * 100 + record.Month >= from);
        }
        if (filters.ToYear is not null)
        {
            var to = filters.ToYear.Value * 100 + Math.Clamp(filters.ToMonth ?? 12, 1, 12);
            query = query.Where(record => record.Year * 100 + record.Month <= to);
        }
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


    private async Task<string> ReadDataFromFile(CancellationToken cancellationToken)
    {
        var configuredPath = config[Configurations.DATA_SAVE_FILE]
            ?? throw new Exception("Save file path is not configured.");
        var path = Path.GetFullPath(configuredPath, AppContext.BaseDirectory);
        return await File.ReadAllTextAsync(path, cancellationToken);
    }
}
