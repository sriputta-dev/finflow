const STATUS_COLORS = {
  Pending:  { bg: "#FFF7ED", text: "#C2410C", dot: "#F97316" },
  Approved: { bg: "#F0FDF4", text: "#15803D", dot: "#22C55E" },
  Declined: { bg: "#FEF2F2", text: "#B91C1C", dot: "#EF4444" },
  Flagged:  { bg: "#FFFBEB", text: "#B45309", dot: "#F59E0B" },
};

export default function TransactionFeed({ transactions }) {
  if (transactions.length === 0) {
    return (
      <div style={{ textAlign: "center", color: "#94A3B8", padding: "40px 0" }}>
        No transactions yet. Submit a payment to see live updates.
      </div>
    );
  }

  return (
    <div style={{ maxHeight: "420px", overflowY: "auto" }}>
      {transactions.map((tx) => {
        const colors = STATUS_COLORS[tx.status] || STATUS_COLORS.Pending;
        return (
          <div
            key={tx.id}
            style={{
              display: "flex",
              justifyContent: "space-between",
              alignItems: "center",
              padding: "12px 16px",
              borderBottom: "1px solid #F1F5F9",
              animation: "fadeIn 0.3s ease",
            }}
          >
            <div>
              <div style={{ fontWeight: 600, fontSize: "14px", color: "#1E293B" }}>
                {tx.senderId} → {tx.receiverId}
              </div>
              <div style={{ fontSize: "12px", color: "#64748B", marginTop: 2 }}>
                {new Date(tx.createdAt).toLocaleTimeString()}
              </div>
            </div>

            <div style={{ textAlign: "right" }}>
              <div style={{ fontWeight: 700, fontSize: "15px", color: "#1A2A4A" }}>
                ${Number(tx.amount).toLocaleString("en-US", { minimumFractionDigits: 2 })} {tx.currency}
              </div>
              <span
                style={{
                  display: "inline-flex",
                  alignItems: "center",
                  gap: 5,
                  padding: "2px 10px",
                  borderRadius: 999,
                  fontSize: "11px",
                  fontWeight: 600,
                  background: colors.bg,
                  color: colors.text,
                  marginTop: 4,
                }}
              >
                <span style={{
                  width: 6, height: 6, borderRadius: "50%",
                  background: colors.dot, display: "inline-block"
                }} />
                {tx.status}
              </span>
            </div>
          </div>
        );
      })}
    </div>
  );
}
