using COEPulse.API.DTO;

namespace COEPulse.API.Services;

public class DataFormatter(ILogger<DataFormatter> logger)
{
    private readonly Dictionary<string, VehicleCategory> _categoryMapping = new()
    {
        { "category a", VehicleCategory.A },
        { "category b", VehicleCategory.B },
        { "category c", VehicleCategory.C },
        { "category d", VehicleCategory.D },
        { "category e", VehicleCategory.E }
    };


    public COERecord[] FormatData(string csvData)
    {
        var lines = csvData.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var records = new List<COERecord>();
        foreach (var _line in lines.Skip(1)) // Skip header
        {
            var line = FormatLine(_line);
            var fields = line.Split(',');
            if (fields.Length < 7) continue;

            COERecord record;
            try
            {
                var dateParts = fields[0].Split("-");
                record = new()
                {
                    Year = int.Parse(dateParts[0]),
                    Month = int.Parse(dateParts[1]),
                    BiddingNumber = int.Parse(fields[1]),
                    VehicleCategory = _categoryMapping[fields[2].ToLower()],
                    Quota = int.Parse(fields[3]),
                    BidsSuccess = int.Parse(fields[4]),
                    BidsReceived = int.Parse(fields[5]),
                    Premium = int.Parse(fields[6])
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


    private static string FormatLine(string line)
    {
        // there are ',' characters between two '"' characters
        // should remove such ',' and '"' characters
        var quoteFound = false;
        var result = string.Empty;

        foreach (var c in line)
        {
            if (c == '"' && !quoteFound)
            {
                quoteFound = true;
            }
            else if (c == '"')
            {
                quoteFound = false;
            }
            else if (!quoteFound || c != ',')
            {
                result += c;
            }
        }

        return result;
    }
}
