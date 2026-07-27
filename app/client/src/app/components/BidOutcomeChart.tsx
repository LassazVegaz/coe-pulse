export default function BidOutcomeChart({
  successful,
  received,
}: {
  successful: number;
  received: number;
}) {
  const successRate = received ? Math.min(100, (successful / received) * 100) : 0;
  const unsuccessful = Math.max(0, received - successful);

  return (
    <div className="outcome-chart">
      <div
        className="pie-chart"
        role="img"
        aria-label={`${successRate.toFixed(1)}% of bids were successful`}
        style={{
          background: `conic-gradient(#45bd63 0 ${successRate}%, #e7a33c ${successRate}% 100%)`,
        }}
      >
        <div>
          <strong>{successRate.toFixed(1)}%</strong>
          <span>successful</span>
        </div>
      </div>
      <div className="outcome-legend">
        <div>
          <i className="successful" />
          <span>Successful</span>
          <strong>{successful.toLocaleString("en-SG")}</strong>
        </div>
        <div>
          <i className="unsuccessful" />
          <span>Unsuccessful</span>
          <strong>{unsuccessful.toLocaleString("en-SG")}</strong>
        </div>
      </div>
    </div>
  );
}
