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
    ILogger<DataService> logger)
{
    private readonly Dictionary<string, VehicleCategory> _categoryMapping = new()
    {
        { "category a", VehicleCategory.A },
        { "category b", VehicleCategory.B },
        { "category c", VehicleCategory.C },
        { "category d", VehicleCategory.D },
        { "category e", VehicleCategory.E }
    };

    private COERecord[]? _records;


    public async Task LoadData()
    {
        await synchronizer.FetchData();
        var csvData = await ReadDataFromFile();
        _records = FormatData(csvData);
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

    private COERecord[] FormatData(string csvData)
    {
        var lines = csvData.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var records = new List<COERecord>();
        foreach (var line in lines.Skip(1)) // Skip header
        {
            var fields = line.Split(',');
            if (fields.Length < 8) continue;

            COERecord record;
            try
            {
                record = new()
                {
                    Year = int.Parse(fields[0]),
                    Month = int.Parse(fields[1]),
                    BiddingNumber = int.Parse(fields[2]),
                    VehicleCategory = _categoryMapping[fields[3].ToLower()],
                    Quota = int.Parse(fields[4]),
                    BidsSuccess = int.Parse(fields[5]),
                    BidsReceived = int.Parse(fields[6]),
                    Premium = int.Parse(fields[7])
                };
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error parsing line: {Line}", line);
                continue;
            }

            records.Add(record);
        }
        return [.. records];
    }
}
