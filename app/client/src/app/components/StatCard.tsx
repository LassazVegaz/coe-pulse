export default function StatCard({
  tone,
  icon,
  label,
  value,
  detail,
}: {
  tone: string;
  icon: string;
  label: string;
  value: string;
  detail: string;
}) {
  return (
    <article className="stat-card">
      <div className="stat-icon" style={{ background: tone }}>
        {icon}
      </div>
      <div>
        <p>{label}</p>
        <strong>{value}</strong>
        <small>{detail}</small>
      </div>
    </article>
  );
}
