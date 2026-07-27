namespace COEPulse.API.DTO;

public enum VehicleCategory
{
    A = 0,
    B = 1,
    C = 2,
    D = 3,
    E = 4,
}

public class COERecord
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int BiddingNumber { get; set; }
    public VehicleCategory VehicleCategory { get; set; }
    public int Quota { get; set; }
    public int BidsSuccess { get; set; }
    public int BidsReceived { get; set; }
    public int Premium { get; set; }
}
