"use server";

import COERecord, { VehicleCategory } from "@/types/coe-record.type";

export type Filters = {
  pageNo: number;
  pageSize: number;
  fromYear?: number;
  toYear?: number;
  categories?: VehicleCategory[];
};

export type QueryResult = {
  records: COERecord[];
  total: number;
  pageNo: number;
  pageSize: number;
};

const getAPIUrl = () => {
  const url = process.env.BACKEND;
  if (!url) throw new Error("BACKEND environment variable is not defined");
  return url.replace(/\/$/, "");
};

export const getData = async (filters: Filters): Promise<QueryResult> => {
  const query = new URLSearchParams({
    pageNo: filters.pageNo.toString(),
    pageSize: filters.pageSize.toString(),
  });
  if (filters.fromYear) query.set("fromYear", filters.fromYear.toString());
  if (filters.toYear) query.set("toYear", filters.toYear.toString());
  filters.categories?.forEach((category) =>
    query.append("categories", VehicleCategory[category]),
  );

  const response = await fetch(`${getAPIUrl()}/api/coe?${query}`, {
    cache: "no-store",
  });
  if (!response.ok)
    throw new Error(`The COE API returned ${response.status}.`);
  return (await response.json()) as QueryResult;
};
