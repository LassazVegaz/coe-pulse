using COEPulse.API.Constants;
using COEPulse.API.DTO;

namespace COEPulse.API.Services;

public record Filters
{
    public int PageNo { get; set; }
    public int PageSize { get; set; }
}

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

    public IEnumerable<COERecord> GetRecords(Filters filters)
    {
        if (_records == null)
            throw new Exception("Data not loaded. Call LoadData() first.");

        return _records.Skip((filters.PageNo - 1) * filters.PageSize).Take(filters.PageSize);
    }


    private async Task<string> ReadDataFromFile()
    {
        var path = config[Configurations.DATA_SAVE_FILE]
            ?? throw new Exception("Save file path is not configured.");
        return await File.ReadAllTextAsync(path);
    }

    private COERecord[] FormatData(string csvData)
    {
        var lines = csvData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        var records = new List<COERecord>();
        foreach (var line in lines.Skip(1)) // Skip header
        {
            var fields = line.Split(',');
            if (fields.Length < 7) continue; // Ensure there are enough fields

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
