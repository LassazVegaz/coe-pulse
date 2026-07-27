import COERecord, { VehicleCategory } from "@/types/coe-record.type";
import { useMemo } from "react";
import colors from "../helpers/colors";

export default function TrendChart({
  records,
  selected,
}: {
  records: COERecord[];
  selected: VehicleCategory[];
}) {
  const series = useMemo(() => {
    const points = records
      .filter((record) => selected.includes(record.vehicleCategory))
      .sort(
        (left, right) =>
          left.year - right.year ||
          left.month - right.month ||
          left.biddingNumber - right.biddingNumber,
      );
    const dates = [
      ...new Set(points.map((item) => `${item.year}-${item.month}`)),
    ].slice(-84);
    const max = Math.max(
      1,
      ...points
        .filter((item) => dates.includes(`${item.year}-${item.month}`))
        .map((item) => item.premium),
    );

    return selected.map((category) => {
      const categoryPoints = dates
        .map((date, index) => {
          const matches = points.filter(
            (item) =>
              item.vehicleCategory === category &&
              `${item.year}-${item.month}` === date,
          );
          if (matches.length === 0) return null;
          const premium =
            matches.reduce((sum, item) => sum + item.premium, 0) /
            matches.length;
          return {
            x: dates.length === 1 ? 50 : (index / (dates.length - 1)) * 100,
            y: 96 - (premium / max) * 88,
          };
        })
        .filter((point): point is { x: number; y: number } => point !== null);
      return { category, categoryPoints };
    });
  }, [records, selected]);

  return (
    <div className="trend-chart" aria-label="COE premium trend">
      <div className="y-labels">
        <span>150K</span>
        <span>100K</span>
        <span>50K</span>
        <span>0</span>
      </div>
      <svg viewBox="0 0 100 100" preserveAspectRatio="none" role="img">
        {[8, 37, 66, 96].map((y) => (
          <line key={y} x1="0" x2="100" y1={y} y2={y} />
        ))}
        {series.map(({ category, categoryPoints }) => (
          <polyline
            key={category}
            points={categoryPoints
              .map((point) => `${point.x},${point.y}`)
              .join(" ")}
            style={{ stroke: colors[category] }}
          />
        ))}
      </svg>
    </div>
  );
}
