"use client";
import COERecord, {
  CATEGORY_LABELS,
  VehicleCategory,
} from "@/types/coe-record.type";
import { useEffect, useMemo, useState } from "react";
import { getData } from "./actions";
import StatCard from "./components/StatCard";
import TrendChart from "./components/TrendChart";
import colors from "./helpers/colors";

const categories = Object.values(VehicleCategory).filter(
  (value): value is VehicleCategory => typeof value === "number",
);

const currency = new Intl.NumberFormat("en-SG", {
  style: "currency",
  currency: "SGD",
  maximumFractionDigits: 0,
});
const number = new Intl.NumberFormat("en-SG");

export default function Home() {
  const [records, setRecords] = useState<COERecord[]>([]);
  const [selected, setSelected] = useState<VehicleCategory[]>(categories);
  const [fromYear, setFromYear] = useState(2018);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  useEffect(() => {
    let active = true;
    getData({ pageNo: 1, pageSize: 2000, fromYear, categories: selected })
      .then((result) => {
        if (active) {
          setRecords(result.records);
          setError(undefined);
        }
      })
      .catch((reason: unknown) => {
        if (active)
          setError(
            reason instanceof Error ? reason.message : "Unable to load data.",
          );
      })
      .finally(() => active && setLoading(false));
    return () => {
      active = false;
    };
  }, [fromYear, selected]);

  const latest = useMemo(() => {
    if (records.length === 0) return [];
    const newest = records.reduce((current, record) =>
      record.year > current.year ||
      (record.year === current.year && record.month > current.month) ||
      (record.year === current.year &&
        record.month === current.month &&
        record.biddingNumber > current.biddingNumber)
        ? record
        : current,
    );
    return records
      .filter(
        (record) =>
          record.year === newest.year &&
          record.month === newest.month &&
          record.biddingNumber === newest.biddingNumber,
      )
      .sort((a, b) => a.vehicleCategory - b.vehicleCategory);
  }, [records]);

  const headline = latest[0];
  const totals = latest.reduce(
    (sum, item) => ({
      quota: sum.quota + item.quota,
      bids: sum.bids + item.bidsReceived,
      success: sum.success + item.bidsSuccess,
    }),
    { quota: 0, bids: 0, success: 0 },
  );
  const demand = totals.quota ? totals.bids / totals.quota : 0;

  const toggleCategory = (category: VehicleCategory) =>
    setSelected((current) =>
      current.includes(category)
        ? current.length === 1
          ? current
          : current.filter((item) => item !== category)
        : [...current, category].sort(),
    );

  return (
    <main className="shell">
      <header className="topbar">
        <div className="brand-mark">🚘</div>
        <div className="brand">
          <h1>COE Pulse SG</h1>
          <p>Track COE trends, demand &amp; premiums</p>
        </div>
        <div className="dataset-status">
          <span className="status-dot">✓</span>
          Dataset live
        </div>
      </header>

      <aside className="sidebar">
        <nav>
          <a className="active" href="#dashboard">
            <span>▦</span> Dashboard
          </a>
          <a href="#results">
            <span>▤</span> Bidding Results
          </a>
          <a href="#trends">
            <span>⌁</span> Trends &amp; Analytics
          </a>
        </nav>
        <section className="filters">
          <div className="section-heading">
            <h2>Filters</h2>
            <button
              onClick={() => {
                setSelected(categories);
                setFromYear(2018);
              }}
            >
              Clear all
            </button>
          </div>
          <label>Vehicle categories</label>
          <div className="category-options">
            {categories.map((category) => (
              <button
                key={category}
                className={selected.includes(category) ? "selected" : ""}
                onClick={() => toggleCategory(category)}
              >
                {CATEGORY_LABELS[category]}
              </button>
            ))}
          </div>
          <label htmlFor="from-year">History from</label>
          <select
            id="from-year"
            value={fromYear}
            onChange={(event) => setFromYear(Number(event.target.value))}
          >
            <option value="2010">2010</option>
            <option value="2015">2015</option>
            <option value="2018">2018</option>
            <option value="2022">2022</option>
          </select>
          <div className="about-data">
            <h3>About the data</h3>
            <p>
              Source:{" "}
              <a
                href="https://data.gov.sg/datasets/d_69b3380ad7e51aff3a7dcc84eba52b8a/view"
                target="_blank"
              >
                data.gov.sg ↗
              </a>
            </p>
            <p>Frequency: twice monthly</p>
          </div>
        </section>
      </aside>

      <div className="dashboard" id="dashboard">
        {error && <div className="error-banner">{error}</div>}
        <section className="stats-grid" aria-busy={loading}>
          <StatCard
            tone="#3b72ec"
            icon="$"
            label="Latest COE premium"
            value={headline ? currency.format(headline.premium) : "—"}
            detail={headline ? CATEGORY_LABELS[headline.vehicleCategory] : ""}
          />
          <StatCard
            tone="#45bd63"
            icon="▦"
            label="Total quota"
            value={number.format(totals.quota)}
            detail="Latest bidding exercise"
          />
          <StatCard
            tone="#8568df"
            icon="♟"
            label="Bids received"
            value={number.format(totals.bids)}
            detail="Across selected categories"
          />
          <StatCard
            tone="#f5a21b"
            icon="✓"
            label="Successful bids"
            value={number.format(totals.success)}
            detail={`${totals.bids ? ((totals.success / totals.bids) * 100).toFixed(1) : 0}% success rate`}
          />
          <StatCard
            tone="#2daec1"
            icon="⌁"
            label="Demand ratio"
            value={`${demand.toFixed(2)}x`}
            detail="Bids received ÷ quota"
          />
        </section>

        <section className="panel trend-panel" id="trends">
          <div className="panel-title">
            <div>
              <h2>COE Premium Trend</h2>
              <p>Monthly average by vehicle category</p>
            </div>
            <span>{fromYear} — latest</span>
          </div>
          <div className="legend">
            {selected.map((category) => (
              <span key={category}>
                <i style={{ background: colors[category] }} />
                {CATEGORY_LABELS[category]}
              </span>
            ))}
          </div>
          <TrendChart records={records} selected={selected} />
        </section>

        <section className="panel details-panel">
          <div className="panel-title">
            <div>
              <h2>Latest Exercise</h2>
              <p>
                {headline
                  ? `${headline.month}/${headline.year} · Exercise ${headline.biddingNumber}`
                  : "Waiting for data"}
              </p>
            </div>
          </div>
          <div className="detail-list">
            <div>
              <span>Quota utilisation</span>
              <strong>
                {totals.quota
                  ? `${((totals.success / totals.quota) * 100).toFixed(1)}%`
                  : "—"}
              </strong>
            </div>
            <div>
              <span>Unsuccessful bids</span>
              <strong>{number.format(totals.bids - totals.success)}</strong>
            </div>
            <div>
              <span>Categories shown</span>
              <strong>{latest.length}</strong>
            </div>
          </div>
        </section>

        <section className="panel results-panel" id="results">
          <div className="panel-title">
            <div>
              <h2>Bidding Results</h2>
              <p>Most recent data by category</p>
            </div>
          </div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Category</th>
                  <th>Quota</th>
                  <th>Bids received</th>
                  <th>Successful</th>
                  <th>Premium</th>
                  <th>Demand</th>
                </tr>
              </thead>
              <tbody>
                {latest.map((record) => (
                  <tr key={record.vehicleCategory}>
                    <td>
                      <span
                        className="category-badge"
                        style={{
                          background: colors[record.vehicleCategory],
                        }}
                      >
                        {CATEGORY_LABELS[record.vehicleCategory]}
                      </span>
                    </td>
                    <td>{number.format(record.quota)}</td>
                    <td>{number.format(record.bidsReceived)}</td>
                    <td>{number.format(record.bidsSuccess)}</td>
                    <td>{currency.format(record.premium)}</td>
                    <td>{(record.bidsReceived / record.quota).toFixed(2)}x</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="panel bars-panel">
          <div className="panel-title">
            <div>
              <h2>Quota vs Bids Received</h2>
              <p>Latest exercise</p>
            </div>
          </div>
          <div className="bar-chart">
            {latest.map((record) => {
              const max = Math.max(record.quota, record.bidsReceived, 1);
              return (
                <div className="bar-group" key={record.vehicleCategory}>
                  <div className="bars">
                    <i
                      className="quota"
                      style={{ height: `${(record.quota / max) * 100}%` }}
                    />
                    <i
                      className="bids"
                      style={{
                        height: `${(record.bidsReceived / max) * 100}%`,
                      }}
                    />
                  </div>
                  <span>{CATEGORY_LABELS[record.vehicleCategory]}</span>
                </div>
              );
            })}
          </div>
          <div className="bar-legend">
            <span>
              <i className="quota" /> Quota
            </span>
            <span>
              <i className="bids" /> Bids received
            </span>
          </div>
        </section>
      </div>
    </main>
  );
}
