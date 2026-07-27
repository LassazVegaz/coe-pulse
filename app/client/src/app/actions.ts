"use server";

import COERecord, { VehicleCategory } from "@/types/coe-record.type";

export type Filters = {
  pageNo: number;
  pageSize: number;
  fromYear?: number;
  fromMonth?: number;
  toYear?: number;
  toMonth?: number;
  categories?: VehicleCategory[];
};

export type QueryResult = {
  records: COERecord[];
  total: number;
  pageNo: number;
  pageSize: number;
};

export type DataResult =
  | { ok: true; data: QueryResult }
  | { ok: false; error: string };

const getAPIUrl = () => {
  const url = process.env.BACKEND;
  if (!url) throw new Error("BACKEND environment variable is not defined");
  return url.replace(/\/$/, "");
};

export const getData = async (filters: Filters): Promise<DataResult> => {
  try {
    const query = new URLSearchParams({
      pageNo: filters.pageNo.toString(),
      pageSize: filters.pageSize.toString(),
    });
    if (filters.fromYear) query.set("fromYear", filters.fromYear.toString());
    if (filters.fromMonth) query.set("fromMonth", filters.fromMonth.toString());
    if (filters.toYear) query.set("toYear", filters.toYear.toString());
    if (filters.toMonth) query.set("toMonth", filters.toMonth.toString());
    filters.categories?.forEach((category) =>
      query.append("categories", VehicleCategory[category]),
    );

    const response = await fetch(`${getAPIUrl()}/api/coe?${query}`, {
      cache: "no-store",
      signal: AbortSignal.timeout(10_000),
    });

    if (!response.ok) {
      return {
        ok: false,
        error:
          response.status >= 500
            ? "The COE data service is temporarily unavailable."
            : "The requested COE data could not be loaded.",
      };
    }

    const data = (await response.json()) as QueryResult;
    if (!Array.isArray(data.records)) {
      return { ok: false, error: "The COE data service returned an unexpected response." };
    }

    return { ok: true, data };
  } catch (error) {
    if (error instanceof Error && error.name === "TimeoutError") {
      return {
        ok: false,
        error: "The COE data service took too long to respond. Please try again.",
      };
    }

    return {
      ok: false,
      error: "Could not connect to the COE data service. Please try again.",
    };
  }
};
