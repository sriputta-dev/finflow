import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  Tooltip, ResponsiveContainer, Cell
} from "recharts";

const STATUS_FILL = {
  Approved: "#22C55E",
  Declined: "#EF4444",
  Flagged:  "#F59E0B",
  Pending:  "#94A3B8",
};

export default function MetricsChart({ transactions }) {
  // Aggregate by status
  const counts = transactions.reduce((acc, tx) => {
    acc[tx.status] = (acc[tx.status] || 0) + 1;
    return acc;
  }, {});

  const data = Object.entries(counts).map(([status, count]) => ({ status, count }));

  if (data.length === 0) {
    return (
      <div style={{ textAlign: "center", color: "#94A3B8", padding: "30px 0" }}>
        Chart will populate as transactions arrive.
      </div>
    );
  }

  return (
    <ResponsiveContainer width="100%" height={200}>
      <BarChart data={data} margin={{ top: 5, right: 10, left: -10, bottom: 5 }}>
        <CartesianGrid strokeDasharray="3 3" stroke="#F1F5F9" />
        <XAxis dataKey="status" tick={{ fontSize: 12 }} />
        <YAxis allowDecimals={false} tick={{ fontSize: 12 }} />
        <Tooltip
          contentStyle={{ borderRadius: 8, border: "1px solid #E2E8F0", fontSize: 13 }}
          cursor={{ fill: "#F8FAFC" }}
        />
        <Bar dataKey="count" radius={[4, 4, 0, 0]}>
          {data.map((entry) => (
            <Cell key={entry.status} fill={STATUS_FILL[entry.status] || "#94A3B8"} />
          ))}
        </Bar>
      </BarChart>
    </ResponsiveContainer>
  );
}
