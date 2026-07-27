export enum VehicleCategory {
  A = 0,
  B = 1,
  C = 2,
  D = 3,
  E = 4,
}

type COERecord = {
  year: number;
  month: number;
  biddingNumber: number;
  vehicleCategory: VehicleCategory;
  quota: number;
  bidsSuccess: number;
  bidsReceived: number;
  premium: number;
};

export default COERecord;
