"use server";

import COERecord from "@/types/coe-record.type";

export type Filters = {
  pageNo: number;
  pageSize: number;
};

const getAPIUrl = () => {
  const url = process.env.BACKEND;
  if (!url) {
    throw new Error("BACKEND environment variable is not defined");
  }
  return url;
};

export const getData = async (filters: Filters) => {
  const queryParams = new URLSearchParams({
    filters: JSON.stringify(filters),
  });
  const api = getAPIUrl();
  const res = await fetch(`${api}?${queryParams.toString()}`);
  return (await res.json()) as COERecord[];
};
