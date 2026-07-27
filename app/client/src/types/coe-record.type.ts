export enum VehicleCategory {
  A = 0,
  B = 1,
  C = 2,
  D = 3,
  E = 4,
}

export const CATEGORY_LABELS: Record<VehicleCategory, string> = {
  [VehicleCategory.A]: "Category A",
  [VehicleCategory.B]: "Category B",
  [VehicleCategory.C]: "Category C",
  [VehicleCategory.D]: "Category D",
  [VehicleCategory.E]: "Category E",
};

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
