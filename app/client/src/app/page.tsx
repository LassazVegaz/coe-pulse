"use client";

import COERecord from "@/types/coe-record.type";
import { useEffect, useState } from "react";
import { getData } from "./actions";

export default function Home() {
  const [data, setData] = useState([] as COERecord[]);

  useEffect(() => {
    let mounted = true;

    const fetchData = async () => {
      const data = await getData({ pageNo: 1, pageSize: 10 });
      if (mounted) setData(data);
    };
    fetchData();

    return () => {
      mounted = false;
    };
  }, []);

  return (
    <div>
      <h1 className="text-center mt-8 text-3xl font-bold">
        Singapore COE Bidding Results
      </h1>

      <div className="overflow-x-auto mt-10">
        <table className="table table-zebra w-full">
          <thead>
            <tr>
              <th>Year</th>
              <th>Month</th>
              <th>Bidding Number</th>
              <th>Vehicle Category</th>
              <th>Quota</th>
              <th>Successful Bids</th>
              <th>Bids Received</th>
              <th>Premium</th>
            </tr>
          </thead>
          <tbody>
            {data.map((record) => (
              <tr
                key={`${record.year}-${record.month}-${record.biddingNumber}-${record.vehicleCategory}`}
              >
                <td>{record.year}</td>
                <td>{record.month}</td>
                <td>{record.biddingNumber}</td>
                <td>{record.vehicleCategory}</td>
                <td>{record.quota}</td>
                <td>{record.bidsSuccess}</td>
                <td>{record.bidsReceived}</td>
                <td>{record.premium}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
